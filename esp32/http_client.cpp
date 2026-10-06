#include "http_client.h"
#include "wifi_manager.h"
#include "config.h"
#include <HTTPClient.h>
#include <ArduinoJson.h>

bool checkCard(const String& cardUid, String& userName) {
    if (!isWiFiConnected()) return false;

    JsonDocument req;
    req["cardUid"] = cardUid;
    String body;
    serializeJson(req, body);

    HTTPClient http;
    http.begin(URL);
    http.addHeader("Content-Type", "application/json");
    http.addHeader("X-Device-Key", DEVICE_KEY);

    int code = http.POST(body);
    String response = http.getString();
    http.end();

    Serial.printf("[check-card] code=%d, response=%s\n", code, response.c_str());

    if (code != 200) return false;

    JsonDocument res;
    if (deserializeJson(res, response)) return false;

    userName = res["userName"] | "";
    return res["accessGranted"] | false;
}