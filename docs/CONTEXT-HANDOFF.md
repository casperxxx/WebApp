# CONTEXT HANDOFF — WebApp (полный)

> Для нового чата: `@docs/CONTEXT-HANDOFF.md` + задача.  
> Обновлено: 2026-11-20.

## 1. Проект одной фразой

Учебный ASP.NET Core API «события + бронирования»: спринты 1→10 от монолита к трём микросервисам (Users / Events / Bookings) + Kafka + Redis Cache-Aside; код и коммиты ведутся **по-студенчески** (просто, близко к ТЗ, русские сообщения/комментарии, без оверинжиниринга).

## 2. Стек и репозиторий

| | |
|--|--|
| Путь | `C:\Projects\WebApp` |
| Git | https://github.com/casperxxx/WebApp |
| Текущая ветка | `sprint-10` (отслеживает `origin/sprint-10`) |
| `main` | на merge sprint-9 (`e4902df`) |
| Стек | .NET 10, ASP.NET Core Web API, EF Core + PostgreSQL, JWT, Kafka (Confluent), Redis (StackExchange.Redis), Docker Compose, xUnit + Moq, Testcontainers (Bookings.IntegrationTests) |
| Dual sync | нет (один репозиторий, одна линия спринтов) |
| CHANGELOG | **нет** — история = `git log` + `README.md` |
| Ревьюер | Илья (замечания после каждой сдачи; типичный коммит «Исправление замечаний от ревьюера») |

### Стиль работы (обязательно помнить)

- Это **учебный** проект курса, не прод.
- Писать и править **«по-студенчески»**: понятные имена, минимум абстракций сверх ТЗ, не «enterprise ради enterprise».
- Коммиты короткие, по смыслу этапа ТЗ; сообщения на русском в стиле уже существующих (`Добавлены…`, `Исправление замечаний…`).
- Коммиты / PR / push — **только по явной просьбе** владельца.
- Не расползаться в рефакторинг соседних спринтов без задачи.

## 3. Правила агента (обязательные)

| Правило | Зачем |
|---------|--------|
| Следовать ТЗ спринта, не «улучшать архитектуру» сверх задания | Сдача и ревью Ильи |
| Сохранять Clean Architecture **внутри** каждого сервиса: Api → Application + Infrastructure; Application → Domain (+ Shared.Contracts у Events/Bookings) | Требование спринтов 7 / 9 |
| Общий JWT: одинаковые `Jwt:Secret` / Issuer / Audience во всех API | Токен Users валиден в Events/Bookings |
| Один Kafka-топик `booking-events`, ключ = EventId | Порядок Confirm/Cancel на событие |
| Redis только в Events; fail-soft (ошибки кеша не роняют HTTP) | ТЗ sprint-10 |
| Инвалидация `event:{id}` **после** SaveChanges | Не закешировать устаревшие места |
| Не коммитить секреты / `.env` | Безопасность |
| Локальные папки старого монолита (`WebApp/`, `WebApp.*`) на диске могут остаться — **в git их нет** | Не возвращать монолит в solution |

## 4. Хронология: что меняли, почему и зачем

Эпохи = спринты. Детали коммитов — в `git log` ветки `sprint-N`.

### 4.1. Sprint-1…2 — каркас Events API

**Зачем:** поднять минимальный API событий по ТЗ (модель, сервис, контроллер, Swagger, README).

| Что | Почему |
|-----|--------|
| Модель Event, EventService, EventsController, route по ТЗ | Базовый CRUD/листинг |
| Middleware ошибок вместо try/catch в контроллере/сервисе | Единый Problem Details |
| Фильтры title/from/to + пагинация | ТЗ sprint-2 |
| Правки после ревью Ильи | Сдача PR #1 / #2 |

### 4.2. Sprint-3…4 — бронирования in-memory + места

**Зачем:** добавить async-бронь со статусами и ограничение мест на событии.

| Что | Почему |
|-----|--------|
| Booking / BookingStatus, BookingStore, BookingService, book endpoint, BackgroundService | Pending → Confirm в фоне |
| TotalSeats / AvailableSeats, `TryReserveSeats` / `ReleaseSeats` | Нельзя подтвердить без мест |
| Защита критической секции + параллельная обработка | Гонки при Confirm |
| Unit-тесты | ТЗ + сдача |

### 4.3. Sprint-5…6 — PostgreSQL + EF + Docker

**Зачем:** убрать in-memory, научить персистентности и миграциям.

| Что | Почему |
|-----|--------|
| Docker Postgres, EF Core, Fluent API, DbContext | Учебный переход на БД |
| Репозитории Event/Booking, миграции | Persistence |
| Каскадное удаление | Связность сущностей |
| Integration tests + Testcontainers | Проверка репозиториев на реальном Postgres |

### 4.4. Sprint-7 — Clean Architecture (монолит слоями)

**Зачем:** разрезать один хост на Domain / Application / Infrastructure / Presentation.

| Что | Почему |
|-----|--------|
| Проекты `WebApp.Domain` / `.Application` / `.Infrastructure`, перенос сущностей, сервисов, EF | ТЗ Clean Architecture |
| Обновление тестовых ProjectReference | Сборка после разреза |

*(Позже, в sprint-9, монолитные проекты удалены из git; solution сейчас только Users/Events/Bookings/Shared.)*

### 4.5. Sprint-8 — Auth JWT + правила броней

**Зачем:** пользователи, роли Admin/User, защита записи событий и броней.

| Что | Почему |
|-----|--------|
| User в Domain, SHA-256 пароля, JWT NuGet, Auth API | Регистрация / login |
| Авторизация эндпоинтов; лимит **10** активных броней (Pending+Confirmed) → 409 | ТЗ |
| Тесты доменных правил auth/бронирования | Сдача |

### 4.6. Sprint-9 — микросервисы + Kafka

**Зачем:** разрезать монолит на три БД/API и связать Bookings↔Events через Kafka.

| Что | Почему |
|-----|--------|
| `Users/` `Events/` `Bookings/` + `Shared.Contracts` | ТЗ микросервисов |
| Порты **5101 / 5102 / 5103**, БД **5434 / 5435 / 5436** | Compose с хоста |
| Outbox в Bookings, consumer в Events, типы Confirmed / Cancelled / Rejected | Согласованность мест и статусов |
| Rejected: нет мест / нет события / **StartAt уже в прошлом** | Асинхронная валидация на стороне Events |
| Полный `docker-compose.yml`, удаление монолита из репо | Стенд «из коробки» |
| PR #9 → merge в `main` | Сдача sprint-9 |

Контракты: `Shared/Shared.Contracts` — `BookingLifecycleEvent`, `BookingEventType`, `KafkaTopics` (топик `booking-events`).

### 4.7. Sprint-10 — Redis Cache-Aside (Events) ← текущая ветка

**Зачем:** кеш чтения событий без ломки API при падении Redis; инвалидация при изменении мест.

| Что | Почему |
|-----|--------|
| `ICacheService`, `CacheKeys`, `RedisCacheOptions`, `RedisCacheService` | Абстракция + StackExchange.Redis |
| Cache-Aside в `EventService` для `GET /events/{id}` и `GET /events/top` | ТЗ |
| Ключи: `event:{id}` TTL **300 с** (invalidate on write); `events:top10` TTL **60 с** (только TTL) | Разный характер данных |
| Redis в compose: образ `redis:7.2-alpine`, контейнер `eventapi-redis`, порт **6379** | ТЗ + совместимость RDB |
| DI: `abortConnect=false`, `connectTimeout/syncTimeout/asyncTimeout=1000` | Илья: без Redis не висеть 10–20 с |
| `IEventCacheGate` / `EventCacheGate` — семафор по ключу | Stampede + гонка с invalidate |
| `IEventCacheInvalidator` — Remove под тем же замком после SaveChanges / Kafka | Не записать stale поверх |
| Unit-тесты Cache-Aside / invalidator / RedisCacheService (~17 в Events.Tests) | ТЗ + замечания ревью |
| README секция «Кеширование (Redis)» | Сдача |

Коммиты на ветке (уже в remote): `a2cb042` → `4b56a31` → `7ff0953` → `ae97e5a`.

**Не закоммичено (правки после ревью Ильи):** gate/invalidator, таймауты Redis, CT/`WaitAsync`, доп. тесты, правки consumer/DI/README — см. §6.

### 4.8. Проверки стенда (диалог 2026-11)

**Зачем:** убедиться, что стенд и замечания Ильи держатся на живом compose.

| Что | Почему / результат |
|-----|-------------------|
| `docker compose up -d --build` + `dotnet test` | APIs up; тесты зелёные (Users/Events/Bookings + Integration) |
| Smoke: register Admin → create future event → GET×2 → redis-cli → book → Confirm → cancel | Кеш пишется; после Confirm ключ сбрасывается; места −/+ |
| Замер latency Redis UP | hit ~2 ms, miss ~4–6 ms |
| Замер Redis DOWN (`docker stop eventapi-redis`) | HTTP 200 fail-soft; ~**3–5 s**/запрос (несколько 1s-таймаутов Get/Set), не 10–20 s |
| Stampede 10 parallel miss | все 200, ~100–130 ms |
| «Сразу Rejected» на старых событиях | **ожидаемо по ТЗ** (StartAt в прошлом), не баг брони |

## 5. Ключевые решения и ловушки

| Тема | Решение | Ловушка, если забыть |
|------|---------|----------------------|
| Стиль кода | Учебный / студенческий | «Прод-рефакторинг» ломает сдачу и тон ревью |
| Бронь past StartAt | Events шлёт Rejected | Кажется, что «booking сломан» |
| Redis недоступен | Логируем, отдаём из PostgreSQL | Без коротких timeout запрос «висит» |
| Miss без Redis | Get (+ повтор под gate) + Set — несколько timeout подряд | ~3–5 s на один GET; это известный нюанс |
| Invalidate vs Cache-Aside | Один gate на ключ | Гонка: Remove, потом stale Set |
| Top-10 | Только TTL 60 s, без invalidate на каждую бронь | Ждать мгновенного обновления топа |
| Redis volume / образ | `7.2-alpine`; при смене мажорной версии — пересоздать volume | RDB version mismatch |
| Порты API | 5101/5102/5103 (не 5001…) | Smoke в «никуда» |
| Kafka внутри Docker | bootstrap `kafka:29092`; с хоста `localhost:9092` | Consumer не коннектится |
| JWT Secret | ≥ 32 байт UTF-8, одинаковый везде | 401 на Events/Bookings |
| Коммиты | Только по просьбе пользователя | Лишние коммиты агента |

## 6. Открытые / следующие шаги

- [ ] **Закоммитить** незакоммиченные фиксы ревью sprint-10 (gate, invalidator, timeouts, тесты, README) — когда попросит владелец
- [ ] При необходимости **PR sprint-10** в `main` / сдача Илье (после коммита)
- [ ] Опционально: ускорить degrade без Redis (не делать Set после уже failed Get / короткий circuit) — **не ТЗ**, только если ревьюер попросит
- [ ] Локальный мусор на диске: папки старого монолита `WebApp*`, `EventApi.IntegrationTests` — не в git; можно удалить вручную, не путать с текущим solution

## 7. Как продолжить в новом аккаунте

1. Клонировать/открыть `casperxxx/WebApp`, ветка `sprint-10`.
2. В новом чате прикрепить: `@docs/CONTEXT-HANDOFF.md` и сформулировать задачу.
3. Поднять стенд: `docker compose up -d --build` → Swagger `:5101` `:5102` `:5103`.
4. Тесты: `dotnet test WebApp.sln`.
5. Помнить: **студенческий стиль**, коммиты только по просьбе.

Примеры промптов:

- `@docs/CONTEXT-HANDOFF.md Закоммить правки ревью sprint-10 студенческим сообщением и покажи diff.`
- `@docs/CONTEXT-HANDOFF.md Подними стенд и проверь Cache-Aside как в §4.8.`
- `@docs/CONTEXT-HANDOFF.md Сверь текущий код Events.Caching с ТЗ sprint-10; ничего не коммить.`

Skills (живут в профиле Windows `~/.cursor/skills`, **не в git** — на другой ПК копировать отдельно):

- `project-context-handoff` — этот документ
- при C#-работе по желанию: `csharp-pro`, `clean-architecture-refactor` (не ломая студенческий тон)

## 8. Карта важных путей

| Область | Путь |
|---------|------|
| Solution / Compose / README | `WebApp.sln`, `docker-compose.yml`, `README.md` |
| Контракты Kafka | `Shared/Shared.Contracts/` |
| Users API | `Users/Users.Api/` |
| Events API + Redis | `Events/Events.Api/`, `Events/Events.Infrastructure/Caching/` |
| Cache-Aside / gate | `Events/Events.Application/Services/EventService.cs`, `Events/Events.Application/Caching/` |
| Kafka consumer + invalidate | `Events/Events.Infrastructure/Messaging/BookingEventsConsumer.cs` |
| Bookings outbox / book | `Bookings/Bookings.Infrastructure/`, `Bookings/Bookings.Api/Controllers/` |
| Тесты кеша | `Events/Events.Tests/EventServiceCacheTests.cs`, `EventCacheInvalidatorTests.cs`, `RedisCacheServiceTests.cs` |
| Этот handoff | `docs/CONTEXT-HANDOFF.md` |

## 9. Как читать всю историю правок

Отдельного `CHANGELOG` в репозитории **нет**.

1. `git log --oneline` (или по ветке `sprint-N`) — идти по сообщениям коммитов как по заголовкам эпох.
2. Свежий обзор поведения и портов — `README.md` (особенно «Кеширование (Redis)» и сценарий проверки).
3. Handoff **не** дублирует каждый коммит: только смысл эпох, решения и ловушки. Поштучные «было→стало» — в git.
)
