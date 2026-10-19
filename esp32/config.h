#ifndef CONFIG_H
#define CONFIG_H

#define SCALE_COUNT 2

extern const char* SSID;
extern const char* PASSWORD;
extern const char* URL;
extern const char* DEVICE_KEY;
extern const char* CELL_IDS[SCALE_COUNT];

#define OLED_SDA 16
#define OLED_SCL 17
#define SCREEN_ADDRESS 0x3C

#define RFID_RST_PIN 22
#define RFID_SS_PIN  21

#define HX711_1_DT_PIN  13
#define HX711_1_SCK_PIN 14
#define HX711_2_DT_PIN  35
#define HX711_2_SCK_PIN 33

#define SCALE_1_FACTOR -384.78
#define SCALE_2_FACTOR -381.0

#define RELAY_PIN 27
#define LOCK_OPEN_MS 3000
#define HEARTBEAT_MS 10000

#define DOOR_SENSOR_ENABLED 1
#define DOOR_PIN 12
#define DOOR_CLOSED_LEVEL LOW

#define SESSION_MS 15000
#define SESSION_MAX_MS 120000
#define DOOR_OPEN_WAIT_MS 5000
#define DOOR_ALARM_MS 60000
#define STABLE_TOLERANCE_GRAMS 2.0
#define STABLE_TIME_MS 2000
#define STABLE_MAX_WAIT_MS 10000

#define SESSION_RETRY_MS 15000
#define FIRMWARE_VERSION "1.4.0"

#define LED_PIN 5
#define LED_COUNT 8
#define LED_BRIGHTNESS 40

#define LED_WIFI        0
#define LED_RFID        1
#define LED_SCALE_FIRST 2
#define LED_RELAY       4
#define LED_DOOR        5
#define LED_SESSION     6
#define LED_STATUS      7

#endif
