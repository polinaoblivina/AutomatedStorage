#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#include <WiFi.h>

void connectToWiFi(const char* ssid, const char* password);
void wifiMaintain();
bool isWiFiConnected();

#endif
