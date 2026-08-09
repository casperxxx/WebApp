# WebApp

API для работы с событиями и бронированиями. Всё хранится в памяти, после перезапуска данные пропадают.

Ссылка на репозиторий: https://github.com/casperxxx/WebApp  
Рабочая ветка: sprint-3

## Как запустить

1. Склонировать репозиторий
2. Зайти в папку WebApp
3. Выполнить команды:

```
dotnet restore
dotnet build
dotnet run --project WebApp/WebApp.csproj
```

Сайт откроется на http://localhost:5176

Swagger: http://localhost:5176/swagger

## Как запустить тесты

```
dotnet test
```

## Методы API

События:
- GET /events — получить события с фильтрацией и пагинацией (200)
- GET /events/{id} — получить одно событие (200 или 404)
- POST /events — создать событие (201)
- PUT /events/{id} — изменить событие (200, 400 или 404)
- DELETE /events/{id} — удалить событие (204 или 404)
- POST /events/{id}/book — создать бронь на событие (202 или 404)

Бронирования:
- GET /bookings/{id} — получить бронь по Id (200 или 404)

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
      "endAt": "2026-07-10T12:00:00"
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
- Rejected — бронь отклонена

### POST /events/{id}/book

Сразу возвращает 202 Accepted и бронь в статусе Pending.  
В заголовке Location будет ссылка на бронь, например `/bookings/{bookingId}`.

Если события с таким id нет — 404.

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
1. ищет брони со статусом Pending
2. ждёт пару секунд (имитация внешней системы)
3. ставит статус Confirmed и заполняет processedAt

Поэтому сразу после создания GET вернёт Pending, а через несколько секунд — уже Confirmed.

## Пример сценария

1. Создать событие через POST /events
2. Создать бронь через POST /events/{id}/book — должен быть 202 и Location
3. Сразу сделать GET /bookings/{id} — статус Pending
4. Подождать несколько секунд и снова GET /bookings/{id} — статус Confirmed, processedAt заполнен

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
- 400 — ошибка валидации / некорректные параметры
- 404 — не найдено
- 500 — внутренняя ошибка
