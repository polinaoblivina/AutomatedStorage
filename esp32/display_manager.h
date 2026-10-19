#ifndef DISPLAY_MANAGER_H
#define DISPLAY_MANAGER_H

#include <Arduino.h>

void displayInit();
void displayAccess(bool granted, const String& cardUid, const String& userName);
void displayAll(bool wifi, bool rfid, const float* weights, const bool* scaleOk, int count, const String& footer);

#endif