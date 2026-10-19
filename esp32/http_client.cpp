#include "http_client.h"
#include "wifi_manager.h"
#include "config.h"
#include <HTTPClient.h>
#include <ArduinoJson.h>

static int postJson(const char* path, const String& body, String& response) {
    if (!isWiFiConnected()) return -1;

    HTTPClient http;
    http.begin(String(URL) + path);
    http.setConnectTimeout(2000);
    http.setTimeout(3000);
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

int sendSessions(const PendingSession& session, String& message) {
    JsonDocument req;
    req["sessionId"] = (const char*)session.id;
    req["cardUid"] = session.cardUid;
    req["ageSeconds"] = (millis() - session.finishedAt) / 1000;

    JsonArray cells = req["cells"].to<JsonArray>();
    for (int i = 0; i < session.cellCount; i++) {
        JsonObject cell = cells.add<JsonObject>();
        cell["cellId"] = session.cells[i].cellId;
        cell["weightBefore"] = session.cells[i].weightBefore;
        cell["weightAfter"] = session.cells[i].weightAfter;
    }

    String body;
    serializeJson(req, body);

    String response;
    int code = postJson("/api/device/sessions", body, response);

    if (code == 200) {
        JsonDocument res;
        if (!deserializeJson(res, response)) message = res["message"] | "";
    }
    return code;
}

bool sendHeartbeat(const float* weights, const bool* scaleOk, bool rfidOk, const char* state) {
    bool allScalesOk = true;
    for (int i = 0; i < SCALE_COUNT; i++) {
        if (!scaleOk[i]) allScalesOk = false;
    }

    JsonDocument req;
    req["weightGrams"] = weights[0];
    req["rfidOk"] = rfidOk;
    req["scaleOk"] = allScalesOk;
    req["state"] = state;
    req["firmwareVersion"] = FIRMWARE_VERSION;

    JsonArray scales = req["scales"].to<JsonArray>();
    for (int i = 0; i < SCALE_COUNT; i++) {
        JsonObject scale = scales.add<JsonObject>();
        scale["cellId"] = CELL_IDS[i];
        scale["weightGrams"] = weights[i];
        scale["ok"] = scaleOk[i];
    }

    String body;
    serializeJson(req, body);

    String response;
    return postJson("/api/device/heartbeat", body, response) == 204;
}
