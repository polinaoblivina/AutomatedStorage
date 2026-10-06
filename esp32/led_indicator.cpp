#include "led_indicator.h"

void ledInit() {
    pinMode(LED_WIFI_PIN, OUTPUT);
    pinMode(LED_RFID_PIN, OUTPUT);
    pinMode(LED_HX711_PIN, OUTPUT);
    pinMode(LED_ERROR_PIN, OUTPUT);
    
    digitalWrite(LED_WIFI_PIN, LOW);
    digitalWrite(LED_RFID_PIN, LOW);
    digitalWrite(LED_HX711_PIN, LOW);
    digitalWrite(LED_ERROR_PIN, LOW);
}

void ledSetWifi(bool on) {
    digitalWrite(LED_WIFI_PIN, on ? HIGH : LOW);
}

void ledSetRfid(bool on) {
    digitalWrite(LED_RFID_PIN, on ? HIGH : LOW);
}

void ledSetHx711(bool on) {
    digitalWrite(LED_HX711_PIN, on ? HIGH : LOW);
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