#include "rfid_reader.h"
#include "config.h"
#include <SPI.h>

static MFRC522 mfrc522(RFID_SS_PIN, RFID_RST_PIN);

bool rfidInit() {
    SPI.begin();
    mfrc522.PCD_Init();
    delay(100);
    
    Serial.println("RC522 initialized");
    return true;
}

bool rfidIsAlive() {
    byte version = mfrc522.PCD_ReadRegister(MFRC522::VersionReg);
    return (version != 0x00 && version != 0xFF);
}

bool rfidReadCard(String& cardId) {
    if (!mfrc522.PICC_IsNewCardPresent()) return false;
    if (!mfrc522.PICC_ReadCardSerial()) return false;
    
    cardId = "";
    for (byte i = 0; i < mfrc522.uid.size; i++) {
        if (mfrc522.uid.uidByte[i] < 0x10) cardId += "0";
        cardId += String(mfrc522.uid.uidByte[i], HEX);
        if (i < mfrc522.uid.size - 1) cardId += ":";
    }
    cardId.toUpperCase();
    
    mfrc522.PICC_HaltA();
    mfrc522.PCD_StopCrypto1();
    
    return true;
}
