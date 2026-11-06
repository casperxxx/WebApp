# WebApp (sprint-9)

Микросервисный API событий и бронирований.  
Три сервиса со своими PostgreSQL, общий JWT (выдаёт Users), связь Bookings → Events через Kafka.

Репозиторий: https://github.com/casperxxx/WebApp  
Ветка: `sprint-9`

## Структура решения

```
WebApp.sln
├── Shared/Shared.Contracts     — BookingConfirmed, BookingCancelled, KafkaTopics
├── Users/                      — Domain / Application / Infrastructure / Api
├── Events/                     — Domain / Application / Infrastructure / Api
└── Bookings/                   — Domain / Application / Infrastructure / Api
```

В каждом сервисе зависимости только «внутрь» (Clean Architecture):  
Api → Application + Infrastructure; Application → Domain (+ Shared.Contracts у Events/Bookings); Infrastructure → Application + Domain.

| Сервис | Порт | БД (хост) | Назначение |
|--------|------|-----------|------------|
| Users | 5101 | `localhost:5434` / `users_db` | регистрация, login, JWT |
| Events | 5102 | `localhost:5435` / `events_db` | CRUD событий, места, Kafka consumer |
| Bookings | 5103 | `localhost:5436` / `bookings_db` | бронь / отмена, фон Confirm, Kafka producer |

Kafka: с хоста `localhost:9092`, внутри Docker `kafka:29092`.  
Zookeeper: `2181` (как в ТЗ).

## Как запустить (Docker — полный стек)

```
docker compose up -d --build
```

Swagger:
- http://localhost:5101/swagger — Users  
- http://localhost:5102/swagger — Events  
- http://localhost:5103/swagger — Bookings  

Остановка: `docker compose down`

Compose поднимает: Zookeeper, Kafka, 3× PostgreSQL, 3× API.  
При старте API накатывают свои EF-миграции.

## Как запустить локально (API на машине)

1. Инфраструктура:

```
docker compose up -d zookeeper kafka postgres-users postgres-events postgres-bookings
```

2. Сборка и три процесса:

```
dotnet restore
dotnet build WebApp.sln

dotnet run --project Users/Users.Api
dotnet run --project Events/Events.Api
dotnet run --project Bookings/Bookings.Api
```

Строки подключения уже в `*/Api/appsettings.json` (порты 5434–5436).

## Роли

- **Admin** — создаёт / меняет / удаляет события; может смотреть и отменять любую бронь  
- **User** — бронирует; смотрит и отменяет только свои брони  

Список событий (`GET /events`) доступен без токена.  
Создание/изменение/удаление событий — только Admin (без токена 401, обычный User 403).  
Бронь и `/bookings` — нужна аутентификация.

Дополнительно:
- не больше **10** активных броней на пользователя (`Pending` + `Confirmed`), иначе 409  
- места на событии списывает **только Events** после Kafka `BookingConfirmed`  
- при отмене **Confirmed**-брони Events возвращает место по `BookingCancelled`

## JWT

Одинаковые `Jwt:Secret` / `Issuer` / `Audience` во всех сервисах (appsettings или env в Docker).

```json
"Jwt": {
  "Secret": "CHANGE_ME_use_user_secrets_min_32_chars!!",
  "Issuer": "WebApp",
  "Audience": "WebApp",
  "ExpirationMinutes": 60
}
```

Секрет не короче 32 байт (UTF-8); иначе сервис не стартует.  
Токен выдаёт только **Users** (`POST /auth/login`). Events и Bookings только проверяют подпись.

### Как получить токен в Swagger

1. Открыть http://localhost:5101/swagger  
2. `POST /auth/register`, например:

```json
{
  "login": "admin1",
  "password": "password",
  "role": "Admin"
}
```

Роль можно не указывать — будет `User`. Для проверки удобно завести и Admin, и User.

3. `POST /auth/login` — в ответе поле `token`  
4. На Events / Bookings: **Authorize** → `Bearer <token>`  
5. Дальше вызывать защищённые методы  

Проверка: без токена создать событие → 401; под User → 403; под Admin — создать событие, забронировать, отменить.

## API

### Users (`:5101`) — без токена

| Метод | Путь | Ответ |
|-------|------|-------|
| POST | `/auth/register` | 204 / 400 |
| POST | `/auth/login` | 200 (`token`) / 400 |

### Events (`:5102`)

| Метод | Путь | Доступ |
|-------|------|--------|
| GET | `/events` | анонимно |
| GET | `/events/{id}` | анонимно |
| POST | `/events` | Admin |
| PUT | `/events/{id}` | Admin |
| DELETE | `/events/{id}` | Admin |

Параметры `GET /events`: `title`, `from`, `to`, `page` (по умолчанию 1), `pageSize` (по умолчанию 10, макс. 100).  
Пример: `GET /events?title=встреча&from=2026-07-01&page=1&pageSize=5`

### Bookings (`:5103`)

| Метод | Путь | Доступ |
|-------|------|--------|
| POST | `/events/{id}/book` | User / Admin → 202 |
| GET | `/bookings/{id}` | владелец или Admin |
| DELETE | `/bookings/{id}` | владелец или Admin (отмена) |

## Модели (кратко)

- **Событие:** id, title, description, startAt, endAt, totalSeats, availableSeats  
- **Пользователь:** id, login, passwordHash (SHA-256), role (User / Admin)  
- **Бронь:** id, eventId, userId, status (`Pending` / `Confirmed` / `Rejected` / `Cancelled`), createdAt, processedAt  

## Фон и Kafka

```
Bookings                    Kafka                         Events
─────────                   ─────                         ──────
POST .../book  → Pending
фон (~2с)      → publish BookingConfirmed  ──►  consumer → −availableSeats
                 (потом Confirm в БД)            processed_bookings (идемпотентность)

DELETE booking → Cancelled
  если была Confirmed → publish BookingCancelled ──► consumer → +availableSeats
                                                processed_cancellations
```

- Топики `booking-confirmed` и `booking-cancelled` создаёт Events при старте  
- Ключ сообщений = `EventId` (порядок по одному событию)  
- Сначала publish, потом `Confirm` в БД: при сбое Kafka бронь остаётся `Pending` и повторится  

Посмотреть сообщения:

```
docker exec webapp-kafka kafka-console-consumer --bootstrap-server localhost:9092 --topic booking-confirmed --from-beginning --timeout-ms 10000
docker exec webapp-kafka kafka-console-consumer --bootstrap-server localhost:9092 --topic booking-cancelled --from-beginning --timeout-ms 10000
```

Группы consumer:

```
docker exec webapp-kafka kafka-consumer-groups --bootstrap-server localhost:9092 --describe --group events-service
docker exec webapp-kafka kafka-consumer-groups --bootstrap-server localhost:9092 --describe --group events-service-cancellations
```

## Миграции

Users:

```
dotnet ef migrations add Имя --project Users/Users.Infrastructure --startup-project Users/Users.Api --output-dir DataAccess/Migrations
```

Events:

```
dotnet ef migrations add Имя --project Events/Events.Infrastructure --startup-project Events/Events.Api --output-dir DataAccess/Migrations
```

Bookings:

```
dotnet ef migrations add Имя --project Bookings/Bookings.Infrastructure --startup-project Bookings/Bookings.Api --output-dir DataAccess/Migrations
```

Применение — при запуске API (`Migrate()`).

## Ошибки

Ответ в формате Problem Details (`application/problem+json`):

| Код | Когда |
|-----|--------|
| 400 | валидация, неверный логин/пароль |
| 401 | нет токена |
| 403 | нет прав |
| 404 | не найдено |
| 409 | лимит броней |
| 500 | внутренняя ошибка |

## Сценарий проверки end-to-end

1. Users: register Admin → login → скопировать `token`  
2. Events / Bookings: Authorize  
3. Events: `POST /events` (даты в будущем, `totalSeats`, например 10)  
4. Bookings: `POST /events/{id}/book` → статус `Pending`  
5. Через ~2–5 с: `GET /bookings/{id}` → `Confirmed`; `GET /events/{id}` → `availableSeats` на 1 меньше  
6. Bookings: `DELETE /bookings/{id}` → через пару секунд места вернулись  

## Dockerfiles

Multi-stage сборка из корня репозитория:

- `Users/Users.Api/Dockerfile`  
- `Events/Events.Api/Dockerfile`  
- `Bookings/Bookings.Api/Dockerfile`  
