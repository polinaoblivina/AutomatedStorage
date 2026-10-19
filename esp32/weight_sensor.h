#ifndef WEIGHT_SENSOR_H
#define WEIGHT_SENSOR_H

#include <Arduino.h>

void weightInit();
void weightUpdate(unsigned long waitMs = 0);
bool weightIsAlive(int scale);
float weightGet(int scale);
bool weightIsStable(int scale, unsigned long forMs);

#endif
