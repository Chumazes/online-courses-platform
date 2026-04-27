# OnlineCourses

OnlineCourses - учебный проект платформы онлайн-курсов. Система состоит из ASP.NET Core WebAPI, слоя доступа к данным на Entity Framework Core, общих моделей, WPF desktop-клиента, React/Vite web-клиента и тестового проекта.

## Стек

- C# / .NET 8
- ASP.NET Core WebAPI
- Entity Framework Core
- SQLite для локальной разработки
- PostgreSQL как основной серверный провайдер
- WPF desktop client
- React + Vite web client
- xUnit + Moq + coverlet для автотестов
- Swagger
- JWT authentication
- Serilog

## Структура проекта

```text
OnlineCourses.API      - REST API, контроллеры, JWT, Swagger, файлы, сидирование SQLite
OnlineCourses.Data     - DbContext, миграции EF Core, репозитории
OnlineCourses.Models   - сущности и DTO
OnlineCourses.Client   - клиентская библиотека для вызовов API
OnlineCourses.Desktop  - WPF-приложение
OnlineCourses.Web      - React/Vite web-клиент
OnlineCourses.Tests    - xUnit + Moq тесты
docs                   - QA-документация, тест-кейсы, чек-листы, Postman-коллекция
```

## Локальная база данных

Для локальной разработки используется SQLite:

```text
OnlineCourses.API/online-courses-dev.db
```

Development-конфигурация уже настроена на SQLite:

```json
{
  "DatabaseProvider": "Sqlite",
  "ConnectionStrings": {
    "SqliteConnection": "Data Source=online-courses-dev.db"
  }
}
```

При запуске API база создается автоматически и заполняется тестовыми данными.

Тестовые пользователи:

```text
teacher@local.dev / 123456
student@local.dev / 123456
student2@local.dev / 123456
admin@local.dev / 123456
```

## Запуск API

Команды выполняются из корня проекта:

```powershell
cd online-courses-platform
dotnet run --project OnlineCourses.API --launch-profile http
```

Swagger:

```text
http://localhost:5064/swagger
```

Базовый URL API:

```text
http://localhost:5064/api
```

## Запуск Desktop

Команда выполняется из корня проекта:

```powershell
dotnet run --project OnlineCourses.Desktop
```

## Запуск React

```powershell
cd OnlineCourses.Web
npm install
npm run dev
```

## Запуск тестов

Команда выполняется из корня проекта:

```powershell
dotnet test OnlineCourses.Tests\OnlineCourses.Tests.csproj
```

## Проверка проекта

```powershell
dotnet build OnlineCourses.slnx --no-restore
dotnet test OnlineCourses.Tests\OnlineCourses.Tests.csproj
cd OnlineCourses.Web
npm run build
```

## Логирование

В API настроен Serilog: вывод в консоль и запись в файл.

Структура каталогов для логов:

```text
logs/
  api/
  desktop/
  web/
```

API пишет файлы сюда:

```text
logs/api/api-.log
```

Каталоги `logs/api`, `logs/desktop` и `logs/web` остаются в репозитории через `.gitkeep`. Реальные `.log` файлы не коммитятся, потому что они добавлены в `.gitignore`.

## CI/CD

В проекте настроен GitHub Actions workflow:

```text
.github/workflows/dotnet.yml
```

GitHub Actions запускает restore, build и test для backend/test project при push и pull request. Отдельный job проверяет React-клиент через `npm ci` и `npm run build`. Результаты автотестов сохраняются как `.trx` artifact.

## Основные сущности БД

- `Users` - пользователи, роли, профиль, хеш пароля.
- `Courses` - курсы, автор, категория, уровень, рейтинг.
- `Categories` - категории курсов.
- `Sections` - разделы курса.
- `Lessons` - уроки и метаданные прикрепленных файлов.
- `Enrollments` - записи студентов на курсы.
- `LessonProgresses` - прогресс прохождения уроков.
- `Reviews` - отзывы и рейтинг курсов.
- `Tags` - теги курсов.
- `CourseTags` - связь многие-ко-многим между курсами и тегами.
- `RefreshTokens` - refresh-токены пользователей.

## Распределение работы в команде

### Frontend

В рамках клиентской части была реализована работа с двумя интерфейсами платформы:

- WPF desktop-клиент для Windows;
- React/Vite web-клиент как веб-версия платформы.

Основные выполненные задачи:

- реализованы экраны авторизации и регистрации;
- добавлено хранение и использование JWT/refresh token на клиенте;
- реализован каталог курсов с поиском, фильтрами, сортировкой и пагинацией;
- реализованы страницы курса, программы курса, уроков и прогресса;
- добавлен личный кабинет студента с записанными курсами;
- реализован профиль пользователя с загрузкой аватара и редактированием bio;
- реализованы интерфейсы преподавателя и администратора;
- добавлено управление курсами, секциями и уроками;
- реализована работа с файлами уроков: загрузка, открытие и скачивание;
- добавлены отзывы, рейтинг курса и модерация отзывов;
- реализованы страницы аналитики и списка студентов курса;
- выполнена визуальная стилизация проекта в едином стиле Low-Level to Top;
- React-версия получила адаптивную структуру, общую оболочку, роутинг и интеграцию с API.

### Backend

В рамках серверной части была реализована REST API-платформа на ASP.NET Core.

Основные выполненные задачи:

- создана структура backend-проекта на ASP.NET Core WebAPI;
- реализованы основные сущности предметной области: пользователи, курсы, категории, секции, уроки, записи на курсы, прогресс, отзывы, refresh-токены и файлы;
- настроен Entity Framework Core и миграции базы данных;
- реализована поддержка PostgreSQL как основного серверного провайдера;
- добавлена SQLite-конфигурация для локальной разработки;
- реализованы репозитории для работы с данными;
- реализована JWT-аутентификация с access token и refresh token;
- добавлены регистрация, вход, выход и получение текущего пользователя;
- реализованы CRUD-операции для курсов, секций, уроков и категорий;
- реализована запись студентов на курсы и отмена записи;
- реализован прогресс прохождения уроков и курсов;
- реализована система отзывов, рейтингов и модерации;
- реализована загрузка аватаров и файлов уроков;
- добавлена Swagger-документация API;
- добавлено кэширование отдельных запросов;
- настроено логирование через Serilog.

### DevOps/QA

В рамках DevOps/QA части были подготовлены инструменты для проверки, запуска и сопровождения проекта.

Основные выполненные задачи:

- настроена локальная SQLite-база для разработки;
- подготовлены тестовые пользователи и демонстрационные данные;
- добавлены автотесты на xUnit + Moq;
- настроена проверка backend и frontend в GitHub Actions;
- добавлен CI workflow для сборки API, тестов и React-клиента;
- настроены artifacts для результатов тестирования;
- добавлена структура логирования `logs/api`, `logs/desktop`, `logs/web`;
- настроен Serilog для записи логов API в консоль и файл;
- подготовлена QA-документация: чек-листы, тест-кейсы, баг-репорты и Postman-коллекция;
- обновлена документация проекта в README.

## Авторы проекта

Работу выполнили студенты группы 3832:

- Шелепов Георгий
- Кузьменко Иван
- Сербин Данил
