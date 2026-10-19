#ifndef HTTP_CLIENT_H
#define HTTP_CLIENT_H

#include <Arduino.h>
#include "session_queue.h"

bool checkCard(const String& cardUid, String& userName);
int sendSessions(const PendingSession& session, String& message);
bool sendHeartbeat(const float* weights, const bool* scaleOk, bool rfidOk, const char* state);

#endif
