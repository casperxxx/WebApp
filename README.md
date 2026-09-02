# WebApp

API для работы с событиями и бронированиями. Данные хранятся в PostgreSQL через Entity Framework Core.

Ссылка на репозиторий: https://github.com/casperxxx/WebApp  
Рабочая ветка: sprint-5

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

При первом запуске EF Core автоматически создаёт таблицы `events` и `bookings` в базе (`EnsureCreated`).

Строка подключения в `WebApp/appsettings.json`:

```
Host=localhost;Port=5433;Database=eventapi;Username=postgres;Password=postgres
```

Порт **5433** на хосте — чтобы не конфликтовать с локальным PostgreSQL на 5432.

## Как запустить тесты

```
dotnet test
```

В тестах используется InMemory-провайдер EF Core вместо PostgreSQL. Интеграционные тесты (`ErrorResponseTests`) подменяют `AppDbContext` через `CustomWebApplicationFactory` и отключают фоновый сервис.

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

Ответ:

```json
{
  "totalCount": 1,
  "items": [
    {
      "id": "00000000-0000-0000-0000-000000000001",
      "title": "Встреча",
      "description": null,
      "startAt": "2026-07-10T10:00:00",
      "endAt": "2026-07-10T12:00:00",
      "totalSeats": 3,
      "availableSeats": 3
    }
  ],
  "page": 1,
  "pageSize": 5
}
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

Пример ответа:

```json
{
  "id": "11111111-1111-1111-1111-111111111111",
  "eventId": "00000000-0000-0000-0000-000000000001",
  "status": "Pending",
  "createdAt": "2026-08-09T10:00:00Z",
  "processedAt": null
}
```

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

Фоновый сервис — синглтон, а DbContext — scoped. Для работы с БД используется `IServiceScopeFactory`: отдельный scope на каждую бронь.

## Синхронизация

Чтобы при одновременных запросах не было овербукинга, в `BookingService` используется **static SemaphoreSlim** — защищает критическую секцию «проверка мест + создание брони» при async-операциях с БД. Обычный `lock` здесь нельзя, потому что внутри есть `await`.

В фоновом сервисе отдельный `SemaphoreSlim` не нужен: каждая задача работает со своим экземпляром `DbContext` в своём scope.

## Пример сценария (персистентность)

1. Запустить `docker compose up -d` и приложение
2. Создать событие через POST /events
3. Создать бронь через POST /events/{id}/book
4. Проверить GET /bookings/{id} — статус Pending
5. Остановить и снова запустить приложение
6. GET /events и GET /bookings/{id} — данные на месте (хранятся в PostgreSQL)

## Пример сценария (овербукинг)

1. Создать событие через POST /events с `"totalSeats": 3`
2. Три раза вызвать POST /events/{id}/book — все должны вернуть 202 Accepted
3. Четвёртый POST /events/{id}/book — должен вернуть 409 Conflict
4. Подождать несколько секунд и проверить GET /bookings/{id} — статус Confirmed, processedAt заполнен

## Формат ошибки

При ошибках API возвращает Problem Details (RFC 7807), Content-Type: `application/problem+json`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.5",
  "title": "Не найдено",
  "status": 404,
  "detail": "Событие с id ... не найдено",
  "instance": "/events/...",
  "traceId": "00-..."
}
```

При ошибке валидации (400) в ответе также есть поле `errors`.

Коды:
- 400 — ошибка валидации / некорректные параметры (в том числе totalSeats <= 0)
- 404 — не найдено
- 409 — нет свободных мест
- 500 — внутренняя ошибка
