#include "lock_controller.h"
#include "config.h"

static bool opened = false;

void lockInit() {
    pinMode(RELAY_PIN, OUTPUT);
    lockClose();
}

void lockOpen() {
    digitalWrite(RELAY_PIN, HIGH);
    opened = true;
}

void lockClose() {
    digitalWrite(RELAY_PIN, LOW);
    opened = false;
}

bool lockIsOpen() {
    return opened;
}
