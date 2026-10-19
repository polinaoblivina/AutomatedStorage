#include "config.h"
#include "session_queue.h"
#include "wifi_manager.h"
#include "http_client.h"
#include "rfid_reader.h"
#include "led_indicator.h"
#include "display_manager.h"
#include "weight_sensor.h"
#include "lock_controller.h"

enum class CabinetState { Idle, SessionOpen };

CabinetState state = CabinetState::Idle;

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

void startSession(const String& cardId) {
    weightUpdate(200);
    readScales(sessionWeightBefore, sessionScaleOk);
    sessionCardId = cardId;
    sessionStartedAt = millis();

    lockOpen();
    state = CabinetState::SessionOpen;
    Serial.printf("[session] started, waiting %d ms\n", SESSION_MS);
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
    unsigned long elapsed = millis() - sessionStartedAt;

    if (lockIsOpen() && elapsed >= LOCK_OPEN_MS) lockClose();
    if (elapsed >= SESSION_MS) finishSession();
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

void updateStatusLeds() {
    bool scalesOk = true;
    for (int i = 0; i < SCALE_COUNT; i++) {
        bool alive = weightIsAlive(i);
        ledSetScale(i, alive);
        if (scaleUsed(i) && !alive) scalesOk = false;
    }

    ledSetRelay(lockIsOpen());

    if (errorState || !scalesOk) ledBlinkError();
    else ledSetError(false);
}

void setup() {
    Serial.begin(115200);
    delay(1000);

    ledInit();
    displayInit();
    weightInit();
    lockInit();

    connectToWiFi(SSID, PASSWORD);
    ledSetWifi(isWiFiConnected());

    if (rfidInit()) {
        ledSetRfid(true);
    } else {
        ledSetRfid(false);
        errorState = true;
    }
}

void loop() {
    weightUpdate();

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

        displayAll(isWiFiConnected(), rfidIsAlive(), weights, ok, SCALE_COUNT, sessionQueueSize());
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
