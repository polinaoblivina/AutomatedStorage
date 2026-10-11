#include "weight_sensor.h"
#include "config.h"
#include "HX711.h"

static HX711 scale;

void weightInit() {
    scale.begin(HX711_DT_PIN, HX711_SCK_PIN);
    scale.set_scale(-384.78);
    scale.tare();
}

bool weightIsAlive() {
    return scale.is_ready();
}

float weightRead() {
    return scale.get_units(5);
}
