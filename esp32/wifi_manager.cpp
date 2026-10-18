#include "wifi_manager.h"
#include <Arduino.h>

#define WIFI_RETRY_MS 10000

static unsigned long lastAttempt = 0;

void connectToWiFi(const char* ssid, const char* password) {
    WiFi.mode(WIFI_STA);
    WiFi.setAutoReconnect(true);
    WiFi.begin(ssid, password);

    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 30) {
        delay(500);
        attempts++;
    }
    lastAttempt = millis();
}

void wifiMaintain() {
    if (WiFi.status() == WL_CONNECTED) return;
    if (millis() - lastAttempt < WIFI_RETRY_MS) return;

    lastAttempt = millis();
    Serial.println("[wifi] not connected, reconnecting");
    WiFi.reconnect();
}

bool isWiFiConnected() {
    return WiFi.status() == WL_CONNECTED;
}
