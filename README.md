# SmartStorage — умный склад ТМЦ на ESP32

Автоматизированное хранилище товарно-материальных ценностей. Сотрудник прикладывает RFID-карту к шкафу,
контроллер ESP32 открывает ячейку и взвешивает её до и после. Сервер по разнице веса определяет, сколько
единиц взяли или положили, обновляет остаток и записывает операцию в журнал. Администратор видит состояние
шкафов и события в реальном времени.

```
 ┌──────────── Шкаф ────────────┐        HTTP + X-Device-Key       ┌──────────── Сервер ────────────┐
 │ ESP32                        │ ───────────────────────────────▶ │ ASP.NET Core Web API           │
 │  RC522 — RFID-карта          │   check-card, session, heartbeat │  ├─ PostgreSQL (EF Core)       │
 │  HX711 — тензодатчик (вес)   │ ◀─────────────────────────────── │  ├─ JWT + refresh-токены       │
 │  реле — замок ячейки         │        доступ / результат        │  └─ SignalR /hubs/devices      │
 │  OLED, светодиоды            │                                  └───────────────┬────────────────┘
 └──────────────────────────────┘                                                  │ REST + WebSocket
                                                                    ┌──────────────▼────────────────┐
                                                                    │ Веб-панель (браузер)          │
                                                                    └───────────────────────────────┘
```

## Возможности

- Доступ к ячейкам по RFID-картам с проверкой прав сотрудника.
- Автоматический учёт по весу: сколько штук взято или положено.
- Товары: создание, перемещение между ячейками, пополнение, инвентаризация, архив, минимальный остаток.
- Шкафы и ячейки, сотрудники, роли и права.
- Журнал операций с фильтрами и постраничным выводом; статистика для дашборда.
- Мониторинг шкафов: онлайн/офлайн, исправность датчиков, текущий вес, лента событий — с обновлением
  в реальном времени через SignalR.
- Вход по email и паролю: JWT на 15 минут + refresh-токен на 7 дней в HttpOnly-cookie с ротацией; не больше
  10 попыток входа в минуту с одного адреса (счётчик в Redis).
- Redis: кеш сводки на 30 секунд и ограничитель попыток входа. Redis необязателен: без него сервер работает
  с кешем в памяти, а при его отказе берёт данные из базы.

## Структура репозитория

| Папка | Что внутри | Технологии |
|---|---|---|
| `server/` | сервер и API | .NET 10, ASP.NET Core, EF Core, PostgreSQL, Redis, MediatR, FluentValidation, Identity, SignalR |
| `server/tests/` | модульные и интеграционные тесты | xUnit, NSubstitute, WebApplicationFactory |
| `server/frontend/` | веб-панель кладовщика и администратора | React, TypeScript, Vite, Ant Design, SignalR |
| `esp32/` | прошивка контроллера шкафа | ESP32 (Arduino), MFRC522, HX711, SSD1306 — см. [esp32/README.md](esp32/README.md) |

Сервер построен по Clean Architecture:

| Проект | Ответственность |
|---|---|
| `ServerAutomatedStorage.Domain` | сущности и бизнес-правила (расчёт количества по весу, остатки, права ролей) |
| `ServerAutomatedStorage.Application` | сценарии (команды и запросы MediatR), валидация, DTO, поведение кеширования |
| `ServerAutomatedStorage.Infrastructure` | EF Core и PostgreSQL, репозитории, миграции, Identity, JWT, ключи устройств, Redis (кеш и ограничитель частоты) |
| `ServerAutomatedStorage.WebAPI` | контроллеры, SignalR-хаб, мониторинг устройств, обработка ошибок |

Зависимости: `WebAPI → Infrastructure → Application → Domain`.

## Как это работает

1. Сотрудник прикладывает карту → ESP32 отправляет `POST /api/device/check-card`.
2. Сервер проверяет: карта зарегистрирована, сотрудник активен, у роли есть право `OpenCell`.
3. ESP32 взвешивает ячейку, открывает замок, через 15 секунд взвешивает снова и отправляет
   `POST /api/device/session` с весом «до» и «после».
4. Сервер делит разницу веса на вес одной единицы товара, меняет остаток и пишет операцию в журнал.
5. Каждые 10 секунд ESP32 отправляет `POST /api/device/heartbeat`; шкаф считается онлайн, если сервер
   слышал его не более 30 секунд назад.
6. События и статусы шкафов отправляются в веб-панель через SignalR (`/hubs/devices`).

## Роли и права

| Право | Что даёт | Admin | Storekeeper | Employee |
|---|---|:-:|:-:|:-:|
| `OpenCell` | открывать ячейки картой | ✅ | ✅ | ✅ |
| `ManageStock` | товары и остатки | ✅ | ✅ | |
| `ViewJournal` | журнал, статистика, мониторинг шкафов | ✅ | ✅ | |
| `ManageUsers` | сотрудники и роли | ✅ | | |
| `ManageSystem` | шкафы и ячейки | ✅ | | |

Права проверяются по базе на каждом запросе: отключение сотрудника или смена роли действуют сразу.

## Запуск для разработки

Нужны: [.NET SDK 10](https://dotnet.microsoft.com/download), PostgreSQL (проверено на 18). Redis (в Docker) желателен,
но не обязателен — см. раздел «Redis».

### 1. Секреты

Пароли и ключи хранятся в user-secrets и не попадают в git. Из корня репозитория:

```powershell
dotnet user-secrets set "ConnectionStrings:Postgres" "Host=localhost;Port=5432;Database=smartstorage;Username=postgres;Password=..." --project server/ServerAutomatedStorage.WebAPI
dotnet user-secrets set "Jwt:Secret" "<случайная строка не короче 32 символов>" --project server/ServerAutomatedStorage.WebAPI
dotnet user-secrets set "Seed:AdminPassword" "<пароль первого администратора>" --project server/ServerAutomatedStorage.WebAPI
dotnet user-secrets set "Devices:0:Name" "cabinet-1" --project server/ServerAutomatedStorage.WebAPI
dotnet user-secrets set "Devices:0:ApiKey" "<ключ устройства>" --project server/ServerAutomatedStorage.WebAPI
```

Ключ устройства (64 hex-символа; тот же ключ указывается в `esp32/config.cpp`) можно сгенерировать так:

```powershell
$bytes = New-Object byte[] 32; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes); -join ($bytes | ForEach-Object { $_.ToString("x2") })
```

### 2. Запуск

```powershell
dotnet run --project server/ServerAutomatedStorage.WebAPI --launch-profile http
```

При первом запуске в режиме Development база создаётся автоматически: применяются миграции, создаются роли
и администратор `admin@storage.local` с паролем из `Seed:AdminPassword`.

| Адрес | Что там |
|---|---|
| http://localhost:5037/scalar | документация API и проверка запросов (кнопка Authorize → Bearer) |
| http://localhost:5037/health | `Healthy`, если база доступна |
| http://localhost:5037/openapi/v1.json | описание API в формате OpenAPI |

Чтобы сервер был доступен ESP32 из локальной сети, запускайте его на всех интерфейсах:

```powershell
dotnet run --project server/ServerAutomatedStorage.WebAPI --launch-profile http -- --urls "http://0.0.0.0:5037"
```

### 3. Веб-панель

Собрать панель один раз (и после каждого изменения во фронтенде):

```powershell
cd server/frontend
npm install
npm run build
```

Сборка попадает в `server/ServerAutomatedStorage.WebAPI/wwwroot` (папка не коммитится), и сервер сам отдаёт панель:
после `dotnet run` она открывается на http://localhost:5037. Любой адрес, кроме `/api`, `/hubs` и файлов,
возвращает `index.html`, а нужную страницу выбирает React.

Для правки фронтенда удобнее режим разработки: сервер запущен, а во втором терминале `npm run dev`. Панель
откроется на http://localhost:5173 и будет обновляться при сохранении файлов; запросы к `/api` и `/hubs` Vite
пересылает на `http://localhost:5037`.

Войти может пользователь с правом `ViewJournal` (кладовщик или администратор).

| Раздел | Что там |
|---|---|
| Сводка | итоги по складу, что заканчивается, кто и что берёт чаще всего |
| Шкафы | состояние устройств и лента событий в реальном времени |
| Склад | шкафы, ячейки и предметы: добавить, пополнить, пересчитать, убрать |
| Журнал | все операции с фильтром по типу |
| Пользователи | сотрудники, карты, роли, вход в панель (только администратор) |

### 4. Миграции (при изменении модели)

```powershell
dotnet ef migrations add ИмяМиграции --project server/ServerAutomatedStorage.Infrastructure --startup-project server/ServerAutomatedStorage.WebAPI --output-dir Persistence/Migrations
dotnet ef database update --project server/ServerAutomatedStorage.Infrastructure --startup-project server/ServerAutomatedStorage.WebAPI
```

### 5. Redis

Redis ускоряет сводку (`GET /api/statistics/dashboard` кешируется на 30 секунд) и считает попытки входа
(10 в минуту с одного адреса, дальше `429` с заголовком `Retry-After`). Запустить его проще всего в Docker:

```powershell
docker run -d --name smartstorage-redis -p 6379:6379 redis:7-alpine
```

Адрес задаётся в `ConnectionStrings:Redis`; для разработки он уже прописан в
`server/ServerAutomatedStorage.WebAPI/appsettings.Development.json` (`localhost:6379`).

| Ситуация | Что происходит |
|---|---|
| Redis запущен | кеш и счётчик попыток в Redis |
| `ConnectionStrings:Redis` не задан | кеш в памяти процесса, ограничитель отключён (так работают тесты) |
| Строка задана, но Redis недоступен | сервер работает: данные берутся из базы, попытки входа не ограничиваются; каждый запрос примерно на секунду медленнее, в логе предупреждения |

## Тесты

```powershell
dotnet test server/ServerAutomatedStorage.slnx
```

База данных и Redis для тестов не нужны. Модульные тесты проверяют доменные правила (расчёт по весу, ротацию
refresh-токенов), проверку карты, мониторинг устройств и поведение кеширования; интеграционные запускают сервер
в памяти и проверяют защиту эндпоинтов, ключ устройства, ответ `429` на входе и кеш сводки.

## API

| Группа | Эндпоинты | Доступ |
|---|---|---|
| Вход | `POST /api/auth/login` (не больше 10 попыток в минуту с адреса, иначе `429`), `refresh`, `logout`; `GET /api/auth/me`; `POST /api/auth/change-password` | все / вошедший |
| Шкафы и ячейки | `/api/cabinets`, `/api/cabinets/{id}/cells` | чтение — вошедший; изменение — `ManageSystem` |
| Товары | `/api/items`, `.../move`, `.../replenish`, `.../adjust` | чтение — вошедший; изменение — `ManageStock` |
| Сотрудники и роли | `/api/users`, `/api/roles` | `ManageUsers` |
| Журнал и статистика | `GET /api/transactions`, `GET /api/statistics/dashboard` (кеш 30 секунд) | `ViewJournal` |
| Мониторинг | `GET /api/devices`, `GET /api/devices/events`, SignalR `/hubs/devices` | `ViewJournal` |
| Устройство (ESP32) | `POST /api/device/check-card`, `session`, `heartbeat` | заголовок `X-Device-Key` |

Подробное описание запросов и ответов — в Scalar. Ошибки возвращаются в формате ProblemDetails (RFC 9457).

## Прошивка ESP32

Настройка, библиотеки и схема подключения — в [esp32/README.md](esp32/README.md).