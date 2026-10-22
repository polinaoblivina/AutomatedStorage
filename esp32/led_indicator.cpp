#include "led_indicator.h"
#include <Adafruit_NeoPixel.h>

#define BLINK_MS 500

static Adafruit_NeoPixel strip(LED_COUNT, LED_PIN, NEO_GRB + NEO_KHZ800);
static LedState states[LED_COUNT];
static bool blinkOn = false;
static unsigned long lastBlink = 0;
static bool dirty = true;

static uint32_t colorOf(LedState state) {
    switch (state) {
        case LedState::Ok:       return Adafruit_NeoPixel::Color(0, 255, 0);
        case LedState::Fail:     return Adafruit_NeoPixel::Color(255, 0, 0);
        case LedState::Active:   return Adafruit_NeoPixel::Color(0, 0, 255);
        case LedState::Warning:  return Adafruit_NeoPixel::Color(255, 160, 0);
        case LedState::Settling: return Adafruit_NeoPixel::Color(0, 200, 200);
        case LedState::Pending:  return Adafruit_NeoPixel::Color(160, 0, 255);
        case LedState::Alarm:    return blinkOn ? Adafruit_NeoPixel::Color(255, 0, 0) : 0;
        default:                 return 0;
    }
}

static void setLed(int index, LedState state) {
    if (index < 0 || index >= LED_COUNT || states[index] == state) return;
    states[index] = state;
    dirty = true;
}

void ledInit() {
    strip.begin();
    strip.setBrightness(LED_BRIGHTNESS);
    strip.clear();
    strip.show();
}

void ledSetWifi(bool on) {
    setLed(LED_WIFI, on ? LedState::Ok : LedState::Fail);
}

void ledSetRfid(bool on) {
    setLed(LED_RFID, on ? LedState::Ok : LedState::Fail);
}

void ledSetScale(int scale, LedState state) {
    if (scale < 0 || scale >= SCALE_COUNT) return;
    setLed(LED_SCALE_FIRST + scale, state);
}

void ledSetRelay(bool on) {
    setLed(LED_RELAY, on ? LedState::Active : LedState::Off);
}

void ledSetDoor(LedState state) {
    setLed(LED_DOOR, state);
}

void ledSetSession(LedState state) {
    setLed(LED_SESSION, state);
}

void ledSetStatus(LedState state) {
    setLed(LED_STATUS, state);
}

void ledUpdate() {
    if (millis() - lastBlink >= BLINK_MS) {
        lastBlink = millis();
        blinkOn = !blinkOn;
        for (int i = 0; i < LED_COUNT; i++) {
            if (states[i] == LedState::Alarm) dirty = true;
        }
    }

    if (!dirty) return;
    dirty = false;

    for (int i = 0; i < LED_COUNT; i++) {
        strip.setPixelColor(i, colorOf(states[i]));
    }
    strip.show();
}
