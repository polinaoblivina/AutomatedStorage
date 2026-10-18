#include "weight_sensor.h"
#include "config.h"
#include "HX711.h"

#define SCALE_TIMEOUT_MS 500
#define SMOOTHING 0.3f

static HX711 scales[SCALE_COUNT];
static const uint8_t DT_PINS[SCALE_COUNT]  = { HX711_1_DT_PIN, HX711_2_DT_PIN };
static const uint8_t SCK_PINS[SCALE_COUNT] = { HX711_1_SCK_PIN, HX711_2_SCK_PIN };
static const float FACTORS[SCALE_COUNT]    = { SCALE_1_FACTOR, SCALE_2_FACTOR };

static float weights[SCALE_COUNT];
static bool hasValue[SCALE_COUNT];
static unsigned long lastDataAt[SCALE_COUNT];

void weightInit() {
    for (int i = 0; i < SCALE_COUNT; i++) {
        scales[i].begin(DT_PINS[i], SCK_PINS[i]);
        pinMode(DT_PINS[i], INPUT_PULLUP);
        scales[i].set_scale(FACTORS[i]);

        if (scales[i].wait_ready_timeout(1000)) {
            scales[i].tare(10);
            Serial.printf("[scale %d] ready, tared\n", i + 1);
        } else {
            Serial.printf("[scale %d] not found\n", i + 1);
        }
    }
}

void weightUpdate(unsigned long waitMs) {
    for (int i = 0; i < SCALE_COUNT; i++) {
        bool ready = waitMs > 0 ? scales[i].wait_ready_timeout(waitMs) : scales[i].is_ready();
        if (!ready) continue;

        float value = scales[i].get_units(1);
        weights[i] = hasValue[i] ? weights[i] + SMOOTHING * (value - weights[i]) : value;
        hasValue[i] = true;
        lastDataAt[i] = millis();
    }
}

bool weightIsAlive(int scale) {
    if (scale < 0 || scale >= SCALE_COUNT) return false;
    return hasValue[scale] && millis() - lastDataAt[scale] < SCALE_TIMEOUT_MS;
}

float weightGet(int scale) {
    if (scale < 0 || scale >= SCALE_COUNT) return 0;
    return weights[scale];
}
