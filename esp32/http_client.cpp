#include "http_client.h"
#include "wifi_manager.h"
#include "config.h"
#include <HTTPClient.h>
#include <ArduinoJson.h>

static int postJson(const char* path, const String& body, String& response) {
    if (!isWiFiConnected()) return -1;

    HTTPClient http;
    http.begin(String(URL) + path);
    http.addHeader("Content-Type", "application/json");
    http.addHeader("X-Device-Key", DEVICE_KEY);

    int code = http.POST(body);
    response = http.getString();
    http.end();

    Serial.printf("[%s] code=%d, response=%s\n", path, code, response.c_str());
    return code;
}

bool checkCard(const String& cardUid, String& userName) {
    JsonDocument req;
    req["cardUid"] = cardUid;
    String body;
    serializeJson(req, body);

    String response;
    if (postJson("/api/device/check-card", body, response) != 200) return false;

    JsonDocument res;
    if (deserializeJson(res, response)) return false;

    userName = res["userName"] | "";
    return res["accessGranted"] | false;
}

bool sendSession(const String& cardUid, float weightBefore, float weightAfter, String& message) {
    JsonDocument req;
    req["cardUid"] = cardUid;
    req["cellId"] = CELL_ID;
    req["weightBefore"] = weightBefore;
    req["weightAfter"] = weightAfter;
    String body;
    serializeJson(req, body);

    String response;
    if (postJson("/api/device/session", body, response) != 200) return false;

    JsonDocument res;
    if (deserializeJson(res, response)) return false;

    message = res["message"] | "";
    return res["success"] | false;
}