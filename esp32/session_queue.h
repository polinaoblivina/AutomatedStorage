#ifndef SESSION_QUEUE_H
#define SESSION_QUEUE_H

#include <Arduino.h>
#include "config.h"

#define SESSION_ID_LENGTH 32
#define SESSION_QUEUE_SIZE 10

struct CellWeighing {
    const char* cellId;
    float weightBefore;
    float weightAfter;
};

struct PendingSession {
    char id[SESSION_ID_LENGTH + 1];
    String cardUid;
    unsigned long finishedAt;
    int cellCount;
    CellWeighing cells[SCALE_COUNT];
};

void sessionNewId(char* out);
void sessionQueuePush(const PendingSession& session);
bool sessionQueueSendNext();
int sessionQueueSize();

#endif
