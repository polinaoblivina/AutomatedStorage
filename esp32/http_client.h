#ifndef HTTP_CLIENT_H
#define HTTP_CLIENT_H

#include <Arduino.h>

bool checkCard(const String& cardUid, String& userName);
bool sendSession(const String& cardUid, float weightBefore, float weightAfter, String& message);
bool sendHeartbeat(float weightGrams, bool rfidOk, bool scaleOk, const char* state);

#endif