# WebApp

API для работы с событиями и бронированиями. Данные хранятся в PostgreSQL через Entity Framework Core. Схема БД управляется **миграциями** (`Migrate()`), а не `EnsureCreated()`.

Ссылка на репозиторий: https://github.com/casperxxx/WebApp  
Рабочая ветка: sprint-6

## Как запустить

1. Склонировать репозиторий
2. Запустить PostgreSQL в Docker (из корня репозитория):

```
docker compose up -d
```

3. Выполнить команды:

```
dotnet restore
dotnet build
dotnet run --project WebApp/WebApp.csproj
```

Сайт откроется на http://localhost:5176

Swagger: http://localhost:5176/swagger

При старте приложение применяет миграции (`db.Database.Migrate()`): создаются таблицы `events` и `bookings`, если их ещё нет.

Строка подключения в `WebApp/appsettings.json`:

```
Host=localhost;Port=5433;Database=eventapi;Username=postgres;Password=postgres
```

Порт **5433** на хосте — чтобы не конфликтовать с локальным PostgreSQL на 5432.

## Миграции EF Core

Пакет `Microsoft.EntityFrameworkCore.Design` нужен для команд `dotnet ef`.

Создать новую миграцию:

```
dotnet ef migrations add ИмяМиграции --project WebApp/WebApp.csproj --output-dir DataAccess/Migrations
```

Применить миграции можно:
- автоматически при запуске приложения (`Migrate()` в `Program.cs`);
- или вручную:

```
dotnet ef database update --project WebApp/WebApp.csproj
```

Начальная миграция: `InitialCreate` (таблицы `events`, `bookings` и внешний ключ).

## Как запустить тесты

```
dotnet test
```

Нужен **запущенный Docker** — интеграционные тесты поднимают PostgreSQL через Testcontainers.

Что проверяется:
- **WebApp.Tests** — юнит-тесты сервисов на InMemory EF Core; `ErrorResponseTests` подменяют БД через `CustomWebApplicationFactory`
- **EventApi.IntegrationTests** — интеграционные тесты репозиториев на реальном PostgreSQL (Testcontainers): миграции, CRUD, фильтры, пагинация

## Репозитории

Доступ к данным идёт через `IEventRepository` / `IBookingRepository`. Сервисы и фоновый сервис не обращаются к `AppDbContext` напрямую.

## Методы API

События:
- GET /events — получить события с фильтрацией и пагинацией (200)
- GET /events/{id} — получить одно событие (200 или 404)
- POST /events — создать событие (201)
- PUT /events/{id} — изменить событие (200, 400 или 404)
- DELETE /events/{id} — удалить событие (204 или 404)
- POST /events/{id}/book — создать бронь на событие (202, 404 или 409)

Бронирования:
- GET /bookings/{id} — получить бронь по Id (200 или 404)

## Модель события

- id — Id события
- title — название
- description — описание (необязательное)
- startAt / endAt — даты начала и окончания
- totalSeats — общее количество мест (обязательное при создании, должно быть больше 0)
- availableSeats — сколько мест свободно (при создании равно totalSeats)

## GET /events — параметры

- title — поиск по названию (частичное совпадение, без учёта регистра)
- from — события, которые начинаются не раньше указанной даты
- to — события, которые заканчиваются не позже указанной даты
- page — номер страницы (по умолчанию 1, минимум 1)
- pageSize — количество элементов на странице (по умолчанию 10, от 1 до 100)

Пример:

```
GET /events?title=встреча&from=2026-07-01&page=1&pageSize=5
```

## Бронирования

Модель Booking:
- id — Id брони
- eventId — Id события
- status — статус (Pending, Confirmed, Rejected)
- createdAt — когда создали
- processedAt — когда обработали (может быть null)

Статусы:
- Pending — бронь создана, ждёт обработки
- Confirmed — бронь подтверждена
- Rejected — бронь отклонена (например, событие удалили до обработки)

### POST /events/{id}/book

Сразу возвращает 202 Accepted и бронь в статусе Pending.  
В заголовке Location будет ссылка на бронь, например `/bookings/{bookingId}`.

При создании брони уменьшается availableSeats.

Ответы:
- 202 — бронь создана
- 404 — события с таким id нет
- 409 — свободных мест нет (Conflict)

### GET /bookings/{id}

Возвращает текущее состояние брони. Если брони нет — 404.

## Фоновая обработка

В фоне работает BookingBackgroundService:
1. берёт все брони со статусом Pending
2. обрабатывает их параллельно через Task.WhenAll
3. для каждой брони ждёт пару секунд (имитация внешней системы)
4. если событие есть — ставит Confirmed и заполняет processedAt
5. если события уже нет или произошла ошибка — ставит Rejected и возвращает место через ReleaseSeats

Поэтому сразу после создания GET вернёт Pending, а через несколько секунд — уже Confirmed (или Rejected).

Фоновый сервис — синглтон. Репозитории (и DbContext) — scoped, поэтому используется `IServiceScopeFactory`.

## Синхронизация

В `BookingService` используется **static SemaphoreSlim** — критическая секция «проверка мест + создание брони». Обычный `lock` нельзя, потому что внутри есть `await`.

## Пример сценария (проверка через Swagger)

1. Запустить `docker compose up -d` и приложение
2. POST /events — создать событие
3. POST /events/{id}/book — создать бронь
4. GET /events/{id} и GET /bookings/{id} — проверить данные
5. Перезапустить приложение — данные остаются в PostgreSQL

## Формат ошибки

При ошибках API возвращает Problem Details (RFC 7807), Content-Type: `application/problem+json`.

Коды:
- 400 — ошибка валидации / некорректные параметры (в том числе totalSeats <= 0)
- 404 — не найдено
- 409 — нет свободных мест
- 500 — внутренняя ошибка
