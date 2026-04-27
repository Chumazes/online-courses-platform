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

## Логи

API пишет логи в каталог:

```text
OnlineCourses.API/logs/api-.log
```

Реальные `.log` файлы не коммитятся в репозиторий, потому что они добавлены в `.gitignore`.

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

## Роль DevOps/QA

В рамках DevOps/QA части подготовлены локальная SQLite БД, тестовые данные, автотесты xUnit + Moq, проверка сборки, ER-диаграмма, описание нормализации БД, чек-листы тестирования, тест-кейсы, баг-репорты, Postman-коллекция и CI/CD workflow.
