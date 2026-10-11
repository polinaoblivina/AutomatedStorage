#ifndef CONFIG_H
#define CONFIG_H

extern const char* SSID;
extern const char* PASSWORD;
extern const char* URL;
extern const char* DEVICE_KEY;
extern const char* CELL_ID;

#define OLED_SDA 16
#define OLED_SCL 17
#define SCREEN_ADDRESS 0x3C

#define RFID_RST_PIN 22
#define RFID_SS_PIN  21

#define HX711_DT_PIN  13
#define HX711_SCK_PIN 14

#define RELAY_PIN 27

#define SESSION_MS 15000
#define LOCK_OPEN_MS 3000
#define HEARTBEAT_MS 10000
#define FIRMWARE_VERSION "1.1.0"

#define LED_WIFI_PIN  25
#define LED_RFID_PIN  26
#define LED_HX711_PIN 32
#define LED_ERROR_PIN 4

#endif
