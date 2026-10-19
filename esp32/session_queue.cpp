#include "session_queue.h"
#include "http_client.h"
#include "wifi_manager.h"

static PendingSession pending[SESSION_QUEUE_SIZE];
static int head = 0;
static int count = 0;

void sessionNewId(char* out) {
    for (int i = 0; i < SESSION_ID_LENGTH / 8; i++) {
        snprintf(out + i * 8, 9, "%08lx", (unsigned long)random(0x7FFFFFFF));
    }
}

void sessionQueuePush(const PendingSession& session) {
    if (count == SESSION_QUEUE_SIZE) {
        Serial.printf("[queue] full, dropping oldest session %s\n", pending[head].id);
        head = (head + 1) % SESSION_QUEUE_SIZE;
        count--;
    }

    pending[(head + count) % SESSION_QUEUE_SIZE] = session;
    count++;
    Serial.printf("[queue] session %s queued, %d in queue\n", session.id, count);
}

static bool isFinal(int code) {
    if (code >= 200 && code < 300) return true;
    if (code == 401 || code == 408 || code == 429) return false;
    return code >= 400 && code < 500;
}

bool sessionQueueSendNext() {
    if (count == 0 || !isWiFiConnected()) return false;

    PendingSession& session = pending[head];
    String message;
    int code = sendSessions(session, message);

    if (!isFinal(code)) {
        Serial.printf("[queue] session %s not delivered (code=%d), will retry\n", session.id, code);
        return false;
    }

    Serial.printf("[queue] session %s done (code=%d) %s\n", session.id, code, message.c_str());
    head = (head + 1) % SESSION_QUEUE_SIZE;
    count--;
    return true;
}

int sessionQueueSize() {
    return count;
}
