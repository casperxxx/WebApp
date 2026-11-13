# WebApp (sprint-9)

Микросервисный API событий и бронирований.  
Три сервиса со своими PostgreSQL, общий JWT (выдаёт Users), связь Bookings ↔ Events через Kafka.

Репозиторий: https://github.com/casperxxx/WebApp  
Ветка: `sprint-9`

## Структура решения

```
WebApp.sln
├── Shared/Shared.Contracts     — BookingLifecycleEvent, BookingEventType, KafkaTopics
├── Users/                      — Domain / Application / Infrastructure / Api (+ Tests)
├── Events/                     — Domain / Application / Infrastructure / Api (+ Tests)
└── Bookings/                   — Domain / Application / Infrastructure / Api (+ Tests)
```

В каждом сервисе зависимости только «внутрь» (Clean Architecture):  
Api → Application + Infrastructure; Application → Domain (+ Shared.Contracts у Events/Bookings).

| Сервис | Порт | БД (хост) | Назначение |
|--------|------|-----------|------------|
| Users | 5101 | `localhost:5434` / `users_db` | регистрация, login, JWT |
| Events | 5102 | `localhost:5435` / `events_db` | CRUD событий, места, Kafka consumer |
| Bookings | 5103 | `localhost:5436` / `bookings_db` | бронь / отмена, outbox, фон Confirm |

Kafka: с хоста `localhost:9092`, внутри Docker `kafka:29092`.  
Zookeeper: `2181` (как в ТЗ).

## Как запустить (Docker)

```
docker compose up -d --build
```

Swagger:
- http://localhost:5101/swagger — Users  
- http://localhost:5102/swagger — Events  
- http://localhost:5103/swagger — Bookings  

Остановка: `docker compose down`

## Как запустить локально

```
docker compose up -d zookeeper kafka postgres-users postgres-events postgres-bookings
dotnet build WebApp.sln
dotnet run --project Users/Users.Api
dotnet run --project Events/Events.Api
dotnet run --project Bookings/Bookings.Api
```

## Тесты

```
dotnet test WebApp.sln
```

- **Users.Tests** — домен User, AuthService (дубликат логина, неверный пароль)  
- **Events.Tests** — места (TryReserveSeats / ReleaseSeats), ProcessedBooking  
- **Bookings.Tests** — лимит 10, права на просмотр/отмену, outbox при Cancel Confirmed, Seats  
- **Bookings.IntegrationTests** — PostgreSQL через Testcontainers: Confirm/Cancel + outbox в одной транзакции  


## Роли

- **Admin** — CRUD событий; любая бронь  
- **User** — свои брони  

`GET /events` без токена. Запись событий — только Admin. Брони — с JWT.  
Лимит: не больше **10** активных броней (`Pending` + `Confirmed`) на пользователя (409).

## JWT

Одинаковые `Jwt:Secret` / `Issuer` / `Audience` во всех сервисах. Секрет ≥ 32 байт UTF-8.

### Токен в Swagger

1. http://localhost:5101/swagger → `POST /auth/register` / `login`  
2. Authorize на Events/Bookings: `Bearer <token>`

```json
{ "login": "admin1", "password": "password", "role": "Admin" }
```

## API

### Users `:5101`

| Метод | Путь | Ответ |
|-------|------|-------|
| POST | `/auth/register` | 204 / 400 |
| POST | `/auth/login` | 200 (`token`) / 400 |

### Events `:5102`

| Метод | Путь | Доступ |
|-------|------|--------|
| GET | `/events`, `/events/{id}` | анонимно |
| POST / PUT / DELETE | `/events`… | Admin |

Query: `title`, `from`, `to`, `page`, `pageSize`.

### Bookings `:5103`

| Метод | Путь | Доступ |
|-------|------|--------|
| POST | `/events/{id}/book` | JWT → 202 |
| GET / DELETE | `/bookings/{id}` | владелец или Admin |

## Модели

- **Событие:** id, title, description, startAt, endAt, totalSeats, availableSeats  
- **Пользователь:** id, login, passwordHash, role  
- **Бронь:** id, eventId, userId, **seats**, status, createdAt, processedAt  

## Фон, outbox и Kafka

Один топик **`booking-events`**, ключ сообщения = **EventId** (порядок Confirm → Cancel для одного события).

| Type | Кто шлёт | Кто читает | Эффект |
|------|----------|------------|--------|
| `Confirmed` | Bookings (outbox) | Events | −места |
| `Cancelled` | Bookings (outbox) | Events | +места (если уже списывали) |
| `Rejected` | Events | Bookings | статус Rejected (нет мест / нет события / уже началось) |

```
Bookings                              Kafka booking-events                 Events
────────                              --------------------                 ──────
Pending → Confirm + outbox ──► OutboxPublisher → Confirmed ──► −seats
                                                         нет мест ──► Rejected ──► Bookings Rejected
Cancel Confirmed + outbox ──────────► Cancelled ──────────► +seats
```

- Outbox: бронь и сообщение в **одной транзакции**; публикация отдельным фоном  
- Идемпотентность: `processed_bookings` / `processed_cancellations`  
- При ошибке БД у consumer — **Seek** на то же смещение и повтор (offset вперёд не коммитится)  

Посмотреть топик:

```
docker exec webapp-kafka kafka-console-consumer --bootstrap-server localhost:9092 --topic booking-events --from-beginning --timeout-ms 10000
```

## Миграции

```
dotnet ef migrations add Имя --project Users/Users.Infrastructure --startup-project Users/Users.Api --output-dir DataAccess/Migrations
dotnet ef migrations add Имя --project Events/Events.Infrastructure --startup-project Events/Events.Api --output-dir DataAccess/Migrations
dotnet ef migrations add Имя --project Bookings/Bookings.Infrastructure --startup-project Bookings/Bookings.Api --output-dir DataAccess/Migrations
```

Применение — при старте API.

## Ошибки

Problem Details (`application/problem+json`): 400 / 401 / 403 / 404 / 409 / 500.

## Сценарий проверки

1. Register Admin → login → Authorize  
2. `POST /events` (будущие даты, seats)  
3. `POST /events/{id}/book` → через пару секунд Confirmed и `availableSeats − seats`  
4. `DELETE /bookings/{id}` → места возвращаются  
5. При брони на несуществующий eventId → после обработки статус **Rejected**  
