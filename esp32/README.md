# Прошивка контроллера ESP32

Считывает RFID-карту, открывает замок ячейки, взвешивает её до и после и отправляет результат на сервер.

## Настройка

1. Скопируйте `config.cpp.example` в `config.cpp`.
2. Укажите в `config.cpp` Wi-Fi, адрес сервера и ключ устройства (должен совпадать с разделом `Devices` в настройках сервера).

`config.cpp` содержит пароли и не попадает в git.

## Библиотеки

Устанавливаются через Arduino Library Manager:

- Adafruit GFX Library
- Adafruit BusIO
- Adafruit SSD1306
- U8g2_for_Adafruit_GFX
- ArduinoJson 7.4.3
- MFRC522 (miguelbalboa/rfid)
- HX711 (bogde/HX711)

## Подключение

Пины задаются в `config.h`: OLED (SDA 16, SCL 17), RFID (RST 22, SS 21), HX711 (DT 13, SCK 14), реле замка 27, светодиоды 25, 26, 32, 4.
