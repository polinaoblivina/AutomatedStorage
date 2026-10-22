#ifndef LED_INDICATOR_H
#define LED_INDICATOR_H

#include <Arduino.h>
#include "config.h"

enum class LedState { Off, Ok, Fail, Active, Warning, Settling, Pending, Alarm };

void ledInit();
void ledSetWifi(bool on);
void ledSetRfid(bool on);
void ledSetScale(int scale, LedState state);
void ledSetRelay(bool on);
void ledSetDoor(LedState state);
void ledSetSession(LedState state);
void ledSetStatus(LedState state);
void ledUpdate();

#endif
