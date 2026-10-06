#include "wifi_manager.h"
#include <Arduino.h>

void connectToWiFi(const char* ssid, const char* password) {
    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid, password);
    
    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 30) {
        delay(500);
        attempts++;
    }
}

bool isWiFiConnected() {
    return WiFi.status() == WL_CONNECTED;
}