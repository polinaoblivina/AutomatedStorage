#ifndef LED_INDICATOR_H
#define LED_INDICATOR_H

#include <Arduino.h>
#include "config.h"

void ledInit();
void ledSetWifi(bool on);
void ledSetRfid(bool on);
void ledSetScale(int scale, bool on);
void ledSetRelay(bool on);
void ledSetError(bool on);
void ledBlinkError();

#endif
