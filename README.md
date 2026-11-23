# WebApp (sprint-11)

Микросервисный API событий и бронирований.  
Три сервиса со своими PostgreSQL, общий JWT (выдаёт Users), связь Bookings ↔ Events через Kafka.  
В Events — Redis Cache-Aside для `GET /events/{id}` и `GET /events/top`.  
Наблюдаемость: OpenTelemetry (метрики + трейсы), Serilog JSON, Prometheus, Jaeger, Grafana.

Репозиторий: https://github.com/casperxxx/WebApp  
Ветка: `sprint-11`

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
Zookeeper: `2181`  
Redis: с хоста `localhost:6379`, внутри Docker `redis:6379` (кеш Events).  
Мониторинг: Prometheus `:9090`, Jaeger UI `:16686` / OTLP `:4317`, Grafana `:3000`.

## Как запустить (Docker)

```
docker compose up -d --build
```

Swagger:
- http://localhost:5101/swagger — Users  
- http://localhost:5102/swagger — Events  
- http://localhost:5103/swagger — Bookings  

Мониторинг:
- http://localhost:9090 — Prometheus  
- http://localhost:16686 — Jaeger  
- http://localhost:3000 — Grafana (login `admin` / `admin`)  

Остановка: `docker compose down`

## Как запустить локально

```
docker compose up -d zookeeper kafka postgres-users postgres-events postgres-bookings redis jaeger prometheus grafana
dotnet build WebApp.sln
dotnet run --project Users/Users.Api
dotnet run --project Events/Events.Api
dotnet run --project Bookings/Bookings.Api
```

Локально в `appsettings.json` OTLP по умолчанию `http://localhost:4317` (Jaeger). В Docker у Api задано `Otlp__Endpoint=http://jaeger:4317`.

## Тесты

```
dotnet test WebApp.sln
```

- **Users.Tests** — домен User, AuthService (дубликат логина, неверный пароль)  
- **Events.Tests** — места (TryReserveSeats / ReleaseSeats), ProcessedBooking; Cache-Aside hit/miss/invalidate  
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
| GET | `/events`, `/events/top`, `/events/{id}` | анонимно |
| POST / PUT / DELETE | `/events`… | Admin |

Query: `title`, `from`, `to`, `page`, `pageSize`.  
`/events/top` — топ-10 по доле проданных мест `(total − available) / total`.

### Bookings `:5103`

| Метод | Путь | Доступ |
|-------|------|--------|
| POST | `/events/{id}/book` | JWT → 202 |
| GET / DELETE | `/bookings/{id}` | владелец или Admin |

## Модели

- **Событие:** id, title, description, startAt, endAt, totalSeats, availableSeats  
- **Пользователь:** id, login, passwordHash, role  
- **Бронь:** id, eventId, userId, **seats**, status, createdAt, processedAt  

## Наблюдаемость (sprint-11)

Во всех трёх Api подключены OpenTelemetry и Serilog.

| Сигнал | Что | Куда |
|--------|-----|------|
| Метрики | ASP.NET Core (latency / throughput / active) + runtime | scrape Prometheus с `/metrics` |
| Трейсы | входящий HTTP, исходящий HTTP, EF Core (SQL) | Jaeger по OTLP gRPC `:4317` |
| Логи | структурированный JSON (`CompactJsonFormatter`) | stdout контейнера / консоль |

Имена сервисов в телеметрии: `users-api`, `events-api`, `bookings-api` (так же job’ы в Prometheus).

### Как поднять стек мониторинга

Входит в общий compose:

```
docker compose up -d --build
```

Файлы:
- [`prometheus.yml`](prometheus.yml) — scrape `users-api:8080`, `events-api:8080`, `bookings-api:8080`, путь `/metrics`
- [`grafana/provisioning/`](grafana/provisioning/) — datasource Prometheus + подключение дашбордов
- [`grafana/dashboards/aspnet-overview.json`](grafana/dashboards/aspnet-overview.json) — дашборд **ASP.NET Core Overview**

### Порты UI

| Инструмент | URL | Примечание |
|------------|-----|------------|
| Prometheus | http://localhost:9090 | Status → Targets — все `*-api` должны быть **UP** |
| Jaeger | http://localhost:16686 | сервисы `users-api` / `events-api` / `bookings-api`, спаны HTTP и SQL |
| Grafana | http://localhost:3000 | `admin` / `admin`; дашборд с latency, throughput, active requests |

Проверка метрик вручную: `http://localhost:5101/metrics`, `:5102/metrics`, `:5103/metrics`.

На дашборде панели:
- `http_server_request_duration_seconds` (p50 / p95 / p99)
- `http_server_request_duration_seconds_count` (RPS)
- `http_server_active_requests`

Переменная `job` переключает сервис (`users-api` / `events-api` / `bookings-api`).

## Кеширование (Redis)

Паттерн **Cache-Aside** через абстракцию `ICacheService` (Application) и `RedisCacheService` (Infrastructure / StackExchange.Redis).  
Ключи собраны в `CacheKeys`. `IConnectionMultiplexer` — singleton в DI (`abortConnect=false`, `connectTimeout/syncTimeout/asyncTimeout=1000`).

На промахе — `IEventCacheGate` (семафор по ключу): один поток ходит в БД, остальные ждут. Инвалидация (`IEventCacheInvalidator`) идёт под тем же замком, чтобы после Remove не записать устаревшие места поверх.

### Что кешируется и почему

| Ключ | Что | TTL | Стратегия |
|------|-----|-----|-----------|
| `event:{id}` | карточка события | **300 с** | **инвалидация при записи** (`Remove` после SaveChanges) |
| `events:top10` | топ-10 по `(total − available) / total` | **60 с** | **только TTL** |

- **Событие по id** часто меняется (CRUD Admin, места из Kafka). Устаревание заметно → после Update/Delete/Create и после Confirm/Cancel в consumer ключ удаляем. Порядок: сначала БД, потом кеш.
- **Топ-10** — рейтинговый агрегат для главной; небольшое устаревание ок. Явный сброс на каждое бронирование избыточен.
- **TTL:** 5 мин для карточки (баланс свежести и нагрузки), 1 мин для топа (виджет обновляется чаще без лишних DEL).

### Недоступный Redis

Ошибки Get/Set/Remove логируются и **не пробрасываются** клиенту — запрос идёт в PostgreSQL. Короткие таймауты (~1 с) не дают запросу висеть 10–20 с. API стартует даже если Redis ещё не готов.

Конфиг: секция `Redis` в `Events.Api/appsettings.json`; в Docker — `Redis__ConnectionString=redis:6379` (имя сервиса в сети compose).

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
3. `GET /events/{id}` дважды — второй раз из Redis (`docker exec eventapi-redis redis-cli GET event:{id}`)  
4. `GET /events/top` — ключ `events:top10`  
5. `POST /events/{id}/book` → через пару секунд Confirmed и `availableSeats − seats` (кеш `event:{id}` сброшен)  
6. `DELETE /bookings/{id}` → места возвращаются  
7. При брони на несуществующий eventId → после обработки статус **Rejected**  
8. `GET /metrics` на `:5101` / `:5102` / `:5103` — текст Prometheus  
9. Prometheus → Status → Targets — три job’а **UP**  
10. Jaeger — трейсы `users-api` / `events-api` / `bookings-api` (HTTP + SQL)  
11. Grafana — дашборд ASP.NET Core Overview с latency / throughput / active  
