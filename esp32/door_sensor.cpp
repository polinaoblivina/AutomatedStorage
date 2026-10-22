#include "door_sensor.h"
#include "config.h"

#define DOOR_DEBOUNCE_MS 50

static bool closed = true;
static int lastRaw = -1;
static unsigned long rawChangedAt = 0;

void doorInit() {
    if (!DOOR_SENSOR_ENABLED) return;

    pinMode(DOOR_PIN, INPUT_PULLUP);
    lastRaw = digitalRead(DOOR_PIN);
    closed = lastRaw == DOOR_CLOSED_LEVEL;
    Serial.printf("[door] sensor on GPIO%d, door %s\n", DOOR_PIN, closed ? "closed" : "open");
}

void doorUpdate() {
    if (!DOOR_SENSOR_ENABLED) return;

    int raw = digitalRead(DOOR_PIN);
    if (raw != lastRaw) {
        lastRaw = raw;
        rawChangedAt = millis();
        return;
    }

    bool nowClosed = raw == DOOR_CLOSED_LEVEL;
    if (nowClosed != closed && millis() - rawChangedAt >= DOOR_DEBOUNCE_MS) {
        closed = nowClosed;
        Serial.printf("[door] %s\n", closed ? "closed" : "open");
    }
}

bool doorIsClosed() {
    return closed;
}
