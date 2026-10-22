#ifndef DOOR_SENSOR_H
#define DOOR_SENSOR_H

#include <Arduino.h>

void doorInit();
void doorUpdate();
bool doorIsClosed();

#endif
