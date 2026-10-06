#include "config.h"
#include "wifi_manager.h"
#include "http_client.h"
#include "rfid_reader.h"
#include "led_indicator.h"
#include "display_manager.h"
#include "weight_sensor.h"
#include "lock_controller.h"

unsigned long lastDisplay = 0;
unsigned long lastCheck = 0;
unsigned long lastRfidRetry = 0;
String lastCardId = "";
String currentCardId = "";
unsigned long lastCardTime = 0;
bool errorState = false;
bool showAccess = false;
unsigned long accessDisplayUntil = 0;

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
        
        bool wAlive = weightIsAlive();
        float weight = wAlive ? weightRead() : 0;
        
        displayAll(isWiFiConnected(), rfidIsAlive(), weight);
    }
    
    if (showAccess && millis() > accessDisplayUntil) {
        showAccess = false;
    }
    
    if (errorState) ledBlinkError();
    else ledSetError(false);
    
    String cardId;
    if (rfidReadCard(cardId)) {
        unsigned long now = millis();
        
        if (cardId == lastCardId && (now - lastCardTime) < 3000) {
            delay(100);
            return;
        }
        
        lastCardId = cardId;
        lastCardTime = now;
        currentCardId = cardId;
        
        String userName;
        bool granted = checkCard(cardId, userName);
        
        displayAccess(granted, cardId, userName);
        showAccess = true;
        accessDisplayUntil = millis() + 3000;

        if (granted) {
            lockOpen();
            delay(3000);
            lockClose();
        }
    }
    
    delay(100);
}
