# OnlineCourses

OnlineCourses - учебный проект платформы онлайн-курсов. Система состоит из ASP.NET Core WebAPI, слоя доступа к данным на Entity Framework Core, моделей, desktop-клиента WPF, web-клиента React/Vite и тестового проекта xUnit.

## Стек

- C# / .NET 8
- ASP.NET Core WebAPI
- Entity Framework Core
- SQLite для локальной разработки
- PostgreSQL как основной серверный провайдер
- WPF desktop client
- React + Vite web client
- xUnit для тестов
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
OnlineCourses.Tests    - xUnit-тесты
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

```powershell
cd C:\OSTALNOE\kursovka\OnlineCourses.API
dotnet run --launch-profile http
```

Swagger:

```text
http://localhost:5064/swagger
```

Базовый URL API:

```text
http://localhost:5064/api
```

## Запуск тестов

```powershell
cd C:\OSTALNOE\kursovka
dotnet test OnlineCourses.Tests\OnlineCourses.Tests.csproj
```

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

В рамках DevOps/QA части подготовлены локальная SQLite БД, тестовые данные, проверка сборки и тестов, ER-диаграмма, описание нормализации БД, чек-листы тестирования и анализ CI/CD.
