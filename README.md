# Умный склад ТМЦ на ESP32

Автоматизированное хранилище товарно-материальных ценностей: сотрудник прикладывает RFID-карту к шкафу, контроллер ESP32 открывает ячейку и взвешивает её до и после, а сервер по разнице веса определяет, сколько единиц взяли или положили, и записывает операцию в журнал.

## Структура репозитория

| Папка | Что внутри | Технологии |
|---|---|---|
| `server/` | Сервер и API | ASP.NET Core, Clean Architecture (Domain / Application / Infrastructure / WebAPI) |
| `esp32/` | Прошивка контроллера шкафа | ESP32, MFRC522, HX711, SSD1306 |

## Настройки для разработки
Нужен PostgreSQL. Строка подключения хранится в user-secrets (не в git). Выполнить из корня репозитория:

```
    dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=smartstorage;Username=postgres;Password=..." --project server/ServerAutomatedStorage.WebAPI
    dotnet user-secrets set "Seed:AdminPassword" "..." --project server/ServerAutomatedStorage.WebAPI
    dotnet user-secrets set "Jwt:Secret" "..." --project server/ServerAutomatedStorage.WebAPI
```