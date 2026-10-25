# WebApp

API для событий и бронирований. Данные в PostgreSQL, схема через миграции EF Core (`Migrate()`).

Ссылка на репозиторий: https://github.com/casperxxx/WebApp  
Ветка: sprint-8

В спринте 8 добавлены пользователи, JWT и роли Admin / User.

## Структура решения

Четыре проекта (зависимости только «внутрь»):

- **WebApp.Domain** — сущности (`Event`, `Booking`, `User`), исключения
- **WebApp.Application** — сервисы, интерфейсы репозиториев, DTO, фоновый сервис броней
- **WebApp.Infrastructure** — DbContext, миграции, репозитории, хеш пароля, генерация JWT
- **WebApp** — контроллеры, middleware ошибок, Swagger, `Program.cs`

В `Program.cs` вызываются `AddApplication()` и `AddInfrastructure()`.  
Application **не ссылается** на Infrastructure.

## Как запустить

1. Склонировать репозиторий
2. Поднять PostgreSQL:

```
docker compose up -d
```

3. Собрать и запустить:

```
dotnet restore
dotnet build
dotnet run --project WebApp/WebApp.csproj
```

Приложение: http://localhost:5176  
Swagger: http://localhost:5176/swagger

При старте накатываются миграции (таблицы `events`, `bookings`, `users`).

Строка подключения (`WebApp/appsettings.json`):

```
Host=localhost;Port=5433;Database=eventapi;Username=postgres;Password=postgres
```

Порт 5433 — чтобы не пересекаться с локальным Postgres на 5432.

## Роли

- **Admin** — создаёт / меняет / удаляет события, может отменить любую бронь
- **User** — бронирует события и отменяет только свои брони

Список событий (GET) доступен без токена.  
Создание/изменение/удаление событий — только Admin (без токена 401, обычный пользователь 403).  
Бронь и работа с `/bookings` — нужна аутентификация.

Дополнительно при бронировании:
- нельзя бронировать уже начавшееся событие (400)
- не больше 10 активных броней на пользователя (Pending + Confirmed), иначе 409

## JWT

Настройки в `WebApp/appsettings.json`, секция `Jwt`:

```json
"Jwt": {
  "Secret": "MySecretKey_sprint-8_JWT_token!!",
  "Issuer": "WebApp",
  "Audience": "WebApp",
  "ExpirationMinutes": 60
}
```

Секрет должен быть не короче 32 символов.  
В учебном проекте он лежит в конфиге. В реальном проекте лучше вынести в переменные окружения или User Secrets и не светить в git.

### Как получить токен в Swagger

1. Открыть http://localhost:5176/swagger
2. `POST /auth/register` — например:

```json
{
  "login": "admin1",
  "password": "password",
  "role": "Admin"
}
```

Роль можно не указывать — тогда будет User. Для проверки удобно зарегистрировать и Admin, и User.

3. `POST /auth/login` — в ответе поле `token`
4. Кнопка **Authorize** → вставить токен (часто в формате `Bearer <token>`)
5. Дальше вызывать защищённые методы

Проверка: без токена создать событие → 401; под User → 403; под Admin — можно создать событие, забронировать и отменить бронь.

## Миграции

```
dotnet ef migrations add ИмяМиграции --project WebApp.Infrastructure --startup-project WebApp --output-dir DataAccess/Migrations
```

Применение: при запуске приложения или

```
dotnet ef database update --project WebApp.Infrastructure --startup-project WebApp
```

Есть `InitialCreate` и `AddUsersAndBookingUserId` (таблица users и поле UserId у броней).

## Тесты

```
dotnet test
```

Нужен запущенный Docker (Testcontainers для интеграционных тестов).

- **WebApp.Tests** — сервисы, auth, проверки 401/403
- **EventApi.IntegrationTests** — репозитории на PostgreSQL

## API

Auth (без токена):
- POST /auth/register — 204 / 400
- POST /auth/login — 200 (token) / 400

События:
- GET /events, GET /events/{id}
- POST /events, PUT /events/{id}, DELETE /events/{id} — только Admin
- POST /events/{id}/book — нужна аутентификация (202 / 400 / 401 / 404 / 409)

Брони:
- GET /bookings/{id} — нужна аутентификация
- DELETE /bookings/{id} — отмена (своя или любая у Admin)

### Параметры GET /events

- title — поиск по названию
- from / to — фильтр по датам
- page (по умолчанию 1), pageSize (по умолчанию 10, максимум 100)

Пример: `GET /events?title=встреча&from=2026-07-01&page=1&pageSize=5`

## Модели (кратко)

Событие: id, title, description, startAt, endAt, totalSeats, availableSeats.

Пользователь: id, login, passwordHash (SHA-256), role (User / Admin).

Бронь: id, eventId, userId, status (Pending / Confirmed / Rejected / Cancelled), createdAt, processedAt.

## Фон и синхронизация

`BookingBackgroundService` берёт Pending-брони, ждёт пару секунд и ставит Confirmed или Rejected (с возвратом места).  
В `BookingService` для создания брони используется static `SemaphoreSlim` (вместо `lock`, потому что внутри есть `await`).

## Ошибки

Ответ в формате Problem Details (`application/problem+json`):
- 400 — валидация, прошедшее событие, неверный логин/пароль
- 401 — нет токена
- 403 — нет прав
- 404 — не найдено
- 409 — нет мест или лимит броней
- 500 — внутренняя ошибка
