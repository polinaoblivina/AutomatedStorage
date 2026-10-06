#ifndef LOCK_CONTROLLER_H
#define LOCK_CONTROLLER_H

#include <Arduino.h>

void lockInit();
void lockOpen();
void lockClose();
bool lockIsOpen();

#endif
