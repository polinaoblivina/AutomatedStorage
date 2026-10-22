#include "config.h"
#include "session_queue.h"
#include "wifi_manager.h"
#include "http_client.h"
#include "rfid_reader.h"
#include "led_indicator.h"
#include "display_manager.h"
#include "weight_sensor.h"
#include "lock_controller.h"
#include "door_sensor.h"

enum class CabinetState { Idle, SessionOpen };
enum class SessionPhase { WaitDoorOpen, WaitDoorClose, WaitStable };

CabinetState state = CabinetState::Idle;
SessionPhase sessionPhase = SessionPhase::WaitDoorOpen;
unsigned long phaseStartedAt = 0;

unsigned long lastDisplay = 0;
unsigned long lastCheck = 0;
unsigned long lastRfidRetry = 0;
unsigned long lastHeartbeat = 0;
String lastCardId = "";
unsigned long lastCardTime = 0;
bool errorState = false;
bool showAccess = false;
unsigned long accessDisplayUntil = 0;
unsigned long lastQueueTry = 0;
unsigned long queueRetryMs = 0;

String sessionCardId = "";
unsigned long sessionStartedAt = 0;
float sessionWeightBefore[SCALE_COUNT];
bool sessionScaleOk[SCALE_COUNT];

const char* stateName(CabinetState s) {
    return s == CabinetState::SessionOpen ? "SessionOpen" : "Idle";
}

bool scaleUsed(int scale) {
    return strlen(CELL_IDS[scale]) > 0;
}

void readScales(float* weights, bool* ok) {
    for (int i = 0; i < SCALE_COUNT; i++) {
        ok[i] = weightIsAlive(i);
        weights[i] = ok[i] ? weightGet(i) : 0;
    }
}

bool allScalesStable() {
    for (int i = 0; i < SCALE_COUNT; i++) {
        if (!scaleUsed(i) || !weightIsAlive(i)) continue;
        if (!weightIsStable(i, STABLE_TIME_MS)) return false;
    }
    return true;
}

const char* sessionPhaseName(SessionPhase phase) {
    switch (phase) {
        case SessionPhase::WaitDoorOpen:  return "waiting for door to open";
        case SessionPhase::WaitDoorClose: return "door open, waiting for it to close";
        case SessionPhase::WaitStable:    return "door closed, waiting for scales to settle";
    }
    return "";
}

void setSessionPhase(SessionPhase phase) {
    sessionPhase = phase;
    phaseStartedAt = millis();
    Serial.printf("[session] %s\n", sessionPhaseName(phase));
}

bool doorOpenTooLong() {
    return state == CabinetState::SessionOpen
        && sessionPhase == SessionPhase::WaitDoorClose
        && millis() - phaseStartedAt >= DOOR_ALARM_MS;
}

const char* sessionHint() {
    if (!DOOR_SENSOR_ENABLED) return "Идёт сессия";

    switch (sessionPhase) {
        case SessionPhase::WaitDoorOpen:  return "Откройте дверь";
        case SessionPhase::WaitDoorClose: return doorOpenTooLong() ? "ЗАКРОЙТЕ ДВЕРЬ!" : "Закройте дверь";
        case SessionPhase::WaitStable:    return "Подождите...";
    }
    return "";
}

void startSession(const String& cardId) {
    weightUpdate(200);
    readScales(sessionWeightBefore, sessionScaleOk);
    sessionCardId = cardId;
    sessionStartedAt = millis();

    lockOpen();
    state = CabinetState::SessionOpen;
    setSessionPhase(doorIsClosed() ? SessionPhase::WaitDoorOpen : SessionPhase::WaitDoorClose);
}

void finishSession() {
    if (lockIsOpen()) lockClose();

    weightUpdate(200);
    float weightAfter[SCALE_COUNT];
    bool okAfter[SCALE_COUNT];
    readScales(weightAfter, okAfter);

    PendingSession session;
    sessionNewId(session.id);
    session.cardUid = sessionCardId;
    session.finishedAt = millis();
    session.cellCount = 0;

    for (int i = 0; i < SCALE_COUNT; i++) {
        if (!scaleUsed(i)) continue;

        if (!sessionScaleOk[i] || !okAfter[i]) {
            Serial.printf("[session] scale %d not ready, cell skipped\n", i + 1);
            continue;
        }

        CellWeighing& cell = session.cells[session.cellCount];
        cell.cellId = CELL_IDS[i];
        cell.weightBefore = sessionWeightBefore[i];
        cell.weightAfter = weightAfter[i];
        session.cellCount++;

        Serial.printf("[session] scale %d before=%.1f after=%.1f\n", i + 1, cell.weightBefore, cell.weightAfter);
    }

    if (session.cellCount > 0) {
        sessionQueuePush(session);
        lastQueueTry = millis();
        queueRetryMs = sessionQueueSendNext() ? 0 : SESSION_RETRY_MS;
    }

    state = CabinetState::Idle;
}


void updateSession() {
    unsigned long now = millis();
    unsigned long elapsed = now - sessionStartedAt;

    if (lockIsOpen() && (elapsed >= LOCK_OPEN_MS || sessionPhase != SessionPhase::WaitDoorOpen)) lockClose();

    if (elapsed >= SESSION_MAX_MS) {
        Serial.println("[session] time limit reached");
        finishSession();
        return;
    }

    if (!DOOR_SENSOR_ENABLED) {
        if (elapsed >= SESSION_MS && allScalesStable()) finishSession();
        return;
    }

    switch (sessionPhase) {
        case SessionPhase::WaitDoorOpen:
            if (!doorIsClosed()) {
                setSessionPhase(SessionPhase::WaitDoorClose);
            } else if (elapsed >= DOOR_OPEN_WAIT_MS) {
                Serial.println("[session] door was not opened");
                finishSession();
            }
            break;

        case SessionPhase::WaitDoorClose:
            if (doorIsClosed()) setSessionPhase(SessionPhase::WaitStable);
            break;

        case SessionPhase::WaitStable:
            if (!doorIsClosed()) {
                setSessionPhase(SessionPhase::WaitDoorClose);
            } else if (allScalesStable() || now - phaseStartedAt >= STABLE_MAX_WAIT_MS) {
                finishSession();
            }
            break;
    }
}

void handleCard() {
    String cardId;
    if (!rfidReadCard(cardId)) return;

    unsigned long now = millis();
    if (cardId == lastCardId && (now - lastCardTime) < 3000) return;

    lastCardId = cardId;
    lastCardTime = now;

    String userName;
    bool granted = checkCard(cardId, userName);

    displayAccess(granted, cardId, userName);
    showAccess = true;
    accessDisplayUntil = millis() + 3000;

    if (granted) startSession(cardId);
}

void sendHeartbeatIfDue() {
    if (millis() - lastHeartbeat < HEARTBEAT_MS) return;
    lastHeartbeat = millis();

    float weights[SCALE_COUNT];
    bool ok[SCALE_COUNT];
    readScales(weights, ok);

    if (!sendHeartbeat(weights, ok, rfidIsAlive(), stateName(state)))
        Serial.println("[heartbeat] not delivered");
}

LedState sessionLedState() {
    if (state != CabinetState::SessionOpen) return LedState::Off;
    if (!DOOR_SENSOR_ENABLED) return LedState::Active;

    switch (sessionPhase) {
        case SessionPhase::WaitDoorOpen:  return LedState::Active;
        case SessionPhase::WaitDoorClose: return LedState::Warning;
        case SessionPhase::WaitStable:    return LedState::Settling;
    }
    return LedState::Off;
}

LedState doorLedState() {
    if (!DOOR_SENSOR_ENABLED) return LedState::Off;
    if (doorOpenTooLong()) return LedState::Alarm;
    return doorIsClosed() ? LedState::Ok : LedState::Warning;
}

void updateStatusLeds() {
    bool scalesOk = true;
    for (int i = 0; i < SCALE_COUNT; i++) {
        bool alive = weightIsAlive(i);
        if (scaleUsed(i)) {
            ledSetScale(i, alive ? LedState::Ok : LedState::Fail);
            if (!alive) scalesOk = false;
        } else {
            ledSetScale(i, alive ? LedState::Ok : LedState::Off);
        }
    }

    ledSetRelay(lockIsOpen());
    ledSetDoor(doorLedState());
    ledSetSession(sessionLedState());

    if (errorState || !scalesOk || doorOpenTooLong()) ledSetStatus(LedState::Alarm);
    else if (sessionQueueSize() > 0) ledSetStatus(LedState::Pending);
    else ledSetStatus(LedState::Ok);

    ledUpdate();
}

void setup() {
    Serial.begin(115200);
    delay(1000);

    ledInit();
    displayInit();
    weightInit();
    lockInit();
    doorInit();

    connectToWiFi(SSID, PASSWORD);
    ledSetWifi(isWiFiConnected());

    if (rfidInit()) {
        ledSetRfid(true);
    } else {
        ledSetRfid(false);
        errorState = true;
    }

    updateStatusLeds();
}

void loop() {
    weightUpdate();
    doorUpdate();

    if (millis() - lastCheck > 1000) {
        lastCheck = millis();
        wifiMaintain();
        ledSetWifi(isWiFiConnected());
    }

    if (millis() - lastRfidRetry > 10000) {
        lastRfidRetry = millis();
        if (rfidIsAlive()) {
            ledSetRfid(true);
            errorState = false;
        } else {
            ledSetRfid(false);
            errorState = true;
        }
    }

    if (!showAccess && (millis() - lastDisplay > 1000)) {
        lastDisplay = millis();

        float weights[SCALE_COUNT];
        bool ok[SCALE_COUNT];
        readScales(weights, ok);

        String footer = "";
        if (state == CabinetState::SessionOpen) footer = sessionHint();
        else if (sessionQueueSize() > 0) footer = "Не отправлено: " + String(sessionQueueSize());

        displayAll(isWiFiConnected(), rfidIsAlive(), weights, ok, SCALE_COUNT, footer);
    }

    if (showAccess && millis() > accessDisplayUntil) {
        showAccess = false;
    }

    updateStatusLeds();
    sendHeartbeatIfDue();

    if (state == CabinetState::Idle && sessionQueueSize() > 0 && millis() - lastQueueTry >= queueRetryMs) {
        lastQueueTry = millis();
        queueRetryMs = sessionQueueSendNext() ? 0 : SESSION_RETRY_MS;
    }

    if (state == CabinetState::Idle) handleCard();
    else updateSession();

    delay(100);
}
