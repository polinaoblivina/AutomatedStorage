#include "led_indicator.h"

static const uint8_t SCALE_LED_PINS[] = { LED_SCALE1_PIN, LED_SCALE2_PIN };
static const int SCALE_LED_COUNT = sizeof(SCALE_LED_PINS) / sizeof(SCALE_LED_PINS[0]);

void ledInit() {
    const uint8_t pins[] = {
        LED_WIFI_PIN, LED_RFID_PIN, LED_SCALE1_PIN, LED_SCALE2_PIN, LED_RELAY_PIN, LED_ERROR_PIN
    };

    for (uint8_t pin : pins) {
        pinMode(pin, OUTPUT);
        digitalWrite(pin, LOW);
    }
}

void ledSetWifi(bool on) {
    digitalWrite(LED_WIFI_PIN, on ? HIGH : LOW);
}

void ledSetRfid(bool on) {
    digitalWrite(LED_RFID_PIN, on ? HIGH : LOW);
}

void ledSetScale(int scale, bool on) {
    if (scale < 0 || scale >= SCALE_LED_COUNT) return;
    digitalWrite(SCALE_LED_PINS[scale], on ? HIGH : LOW);
}

void ledSetRelay(bool on) {
    digitalWrite(LED_RELAY_PIN, on ? HIGH : LOW);
}

void ledSetError(bool on) {
    digitalWrite(LED_ERROR_PIN, on ? HIGH : LOW);
}

void ledBlinkError() {
    static unsigned long lastBlink = 0;
    static bool state = false;

    if (millis() - lastBlink > 500) {
        lastBlink = millis();
        state = !state;
        digitalWrite(LED_ERROR_PIN, state ? HIGH : LOW);
    }
}
