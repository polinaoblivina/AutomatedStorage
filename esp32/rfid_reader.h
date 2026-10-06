#ifndef RFID_READER_H
#define RFID_READER_H

#include <Arduino.h>
#include <MFRC522.h>

bool rfidInit();
bool rfidIsAlive();
bool rfidReadCard(String& cardId);

#endif