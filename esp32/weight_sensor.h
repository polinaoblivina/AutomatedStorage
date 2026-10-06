#ifndef WEIGHT_SENSOR_H
#define WEIGHT_SENSOR_H

#include <Arduino.h>

void weightInit();
bool weightIsAlive();
float weightRead();

#endif