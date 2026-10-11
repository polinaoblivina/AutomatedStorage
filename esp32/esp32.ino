#include "config.h"
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
float lastWeight = 0;
bool lastScaleOk = false;
String lastCardId = "";
unsigned long lastCardTime = 0;
bool errorState = false;
bool showAccess = false;
unsigned long accessDisplayUntil = 0;

String sessionCardId = "";
unsigned long sessionStartedAt = 0;
float sessionWeightBefore = 0;
bool sessionScaleOk = false;

void startSession(const String& cardId) {
    sessionScaleOk = weightIsAlive();
    sessionWeightBefore = sessionScaleOk ? weightRead() : 0;
    sessionCardId = cardId;
    sessionStartedAt = millis();

    lockOpen();
    state = CabinetState::SessionOpen;
    Serial.printf("[session] started, before=%.1f, waiting %d ms\n", sessionWeightBefore, SESSION_MS);
}

void finishSession() {
    if (lockIsOpen()) lockClose();

    if (sessionScaleOk && weightIsAlive()) {
        float weightAfter = weightRead();
        String message;
        bool ok = sendSession(sessionCardId, sessionWeightBefore, weightAfter, message);
        Serial.printf("[session] before=%.1f after=%.1f ok=%d %s\n",
                      sessionWeightBefore, weightAfter, ok, message.c_str());
    } else {
        Serial.println("[session] scale not ready, session not sent");
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

const char* stateName(CabinetState s) {
    return s == CabinetState::SessionOpen ? "SessionOpen" : "Idle";
}

void sendHeartbeatIfDue() {
    if (millis() - lastHeartbeat < HEARTBEAT_MS) return;
    lastHeartbeat = millis();

    bool ok = sendHeartbeat(lastWeight, rfidIsAlive(), lastScaleOk, stateName(state));
    if (!ok) Serial.println("[heartbeat] not delivered");
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
    if (millis() - lastCheck > 5000) {
        lastCheck = millis();
        bool wifiOk = isWiFiConnected();
        ledSetWifi(wifiOk);
        if (!wifiOk) connectToWiFi(SSID, PASSWORD);
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

        lastScaleOk = weightIsAlive();
        lastWeight = lastScaleOk ? weightRead() : 0;

        displayAll(isWiFiConnected(), rfidIsAlive(), lastWeight);
    }

    if (showAccess && millis() > accessDisplayUntil) {
        showAccess = false;
    }

    if (errorState) ledBlinkError();
    else ledSetError(false);

    sendHeartbeatIfDue();
    
    if (state == CabinetState::Idle) handleCard();
    else updateSession();

    delay(100);
}