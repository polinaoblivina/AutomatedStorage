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
#define SCALE_2_FACTOR 1.0

#define RELAY_PIN 27
#define SESSION_MS 15000
#define LOCK_OPEN_MS 3000
#define HEARTBEAT_MS 10000
#define FIRMWARE_VERSION "1.2.0"

#define LED_WIFI_PIN   25
#define LED_RFID_PIN   26
#define LED_SCALE1_PIN 32
#define LED_SCALE2_PIN 15
#define LED_RELAY_PIN  2
#define LED_ERROR_PIN  4

#endif
