#include "display_manager.h"
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>
#include <U8g2_for_Adafruit_GFX.h>
#include "config.h"

static Adafruit_SSD1306 display(128, 64, &Wire, -1);
static U8G2_FOR_ADAFRUIT_GFX u8g2;

void displayInit() {
    Wire.begin(OLED_SDA, OLED_SCL);
    display.begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS);
    u8g2.begin(display);
    u8g2.setFont(u8g2_font_6x12_t_cyrillic);
    u8g2.setForegroundColor(SSD1306_WHITE);
    display.clearDisplay();
    display.display();
}

void displayAccess(bool granted, const String& cardUid, const String& userName) {
    display.clearDisplay();
    u8g2.setCursor(0, 12);
    
    if (granted) {
        u8g2.print("ДОСТУП РАЗРЕШЁН");
    } else {
        u8g2.print("ДОСТУП ЗАПРЕЩЁН");
    }
    
    u8g2.setCursor(0, 30);
    u8g2.print(userName);
    
    display.display();
}

void displayAll(bool wifi, bool rfid, const float* weights, const bool* scaleOk, int count, int pendingSessions) 
{
    display.clearDisplay();
    u8g2.setCursor(0, 12);
    u8g2.print("WiFi: ");
    u8g2.print(wifi ? "OK" : "--");
    u8g2.print(" RFID: ");
    u8g2.print(rfid ? "OK" : "--");

    for (int i = 0; i < count; i++) {
        u8g2.setCursor(0, 28 + i * 14);
        u8g2.print("Весы ");
        u8g2.print(i + 1);
        u8g2.print(": ");
        if (scaleOk[i]) {
            u8g2.print(weights[i], 1);
            u8g2.print(" г");
        } else {
            u8g2.print("нет связи");
        }
    }

    if (pendingSessions > 0) {
        u8g2.setCursor(0, 28 + count * 14);
        u8g2.print("Не отправлено: ");
        u8g2.print(pendingSessions);
    }

    display.display();
}
