# WebApp

API для работы с событиями. Всё хранится в памяти, после перезапуска данные пропадают.

Ссылка на репозиторий: https://github.com/casperxxx/WebApp  
Рабочая ветка: sprint-1


## Как запустить

1. Склонировать репозиторий
2. Зайти в папку WebApp/WebApp
3. Выполнить команды:

dotnet restore
dotnet build
dotnet run


Сайт откроется на http://localhost:5176


## Что где лежит

- Models — классы Event и EventDTO
- Services — сервис с логикой (EventService)
- Controllers — контроллер с эндпоинтами
- Program.cs — настройка приложения

## Методы API

- GET /events — получить все события (200, если пусто — 404)
- GET /events/{id} — получить одно событие (200 или 404)
- POST /events — создать событие (201)
- PUT /events/{id} — изменить событие (200 или 404)
- DELETE /events/{id} — удалить событие (204 или 404)

