#ifndef HTTP_CLIENT_H
#define HTTP_CLIENT_H

#include <Arduino.h>

bool checkCard(const String& cardUid, String& userName);
bool sendSession(const String& cardUid, const char* cellId, float weightBefore, float weightAfter, String& message);
bool sendHeartbeat(const float* weights, const bool* scaleOk, bool rfidOk, const char* state);

#endif
