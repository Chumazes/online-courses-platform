# API документация: OnlineCourses

## Общая информация

Базовый URL:

```text
http://localhost:5064/api
```

Swagger:

```text
http://localhost:5064/swagger
```

Авторизация:

```text
Authorization: Bearer {access_token}
```

Срок действия токенов:

- Access token: 15 минут
- Refresh token: 7 дней

Роли:

- `student` - просмотр курсов, запись на курс, прогресс, отзывы.
- `teacher` - возможности студента, создание и редактирование своих курсов, разделов и уроков.
- `admin` - возможности преподавателя, управление категориями и модерация отзывов.

## Auth

### POST `/api/Auth/register`

Регистрация пользователя.

Доступ: все.

Пример тела запроса:

```json
{
  "email": "user@test.com",
  "password": "123456",
  "fullName": "Иван Иванов"
}
```

### POST `/api/Auth/login`

Вход пользователя.

Доступ: все.

Пример тела запроса:

```json
{
  "email": "student@local.dev",
  "password": "123456"
}
```

### GET `/api/Auth/me`

Получить профиль текущего пользователя.

Доступ: авторизованный пользователь.

### PUT `/api/Auth/me`

Обновить профиль текущего пользователя.

Доступ: авторизованный пользователь.

### POST `/api/Auth/refresh`

Обновить access token через refresh token.

Доступ: все.

### POST `/api/Auth/logout`

Выйти из текущей сессии.

Доступ: авторизованный пользователь.

### POST `/api/Auth/logout-all`

Завершить все сессии пользователя.

Доступ: авторизованный пользователь.

## Courses

### GET `/api/Courses`

Получить список курсов. Поддерживает публичный просмотр.

Доступ: все.

### GET `/api/Courses/{id}`

Получить курс по идентификатору.

Доступ: все.

### GET `/api/Courses/my`

Получить курсы текущего преподавателя.

Доступ: `teacher`, `admin`.

### POST `/api/Courses`

Создать курс.

Доступ: `teacher`, `admin`.

### PUT `/api/Courses/{id}`

Обновить курс.

Доступ: автор курса, `admin`.

### DELETE `/api/Courses/{id}`

Удалить курс.

Доступ: автор курса, `admin`.

### GET `/api/Courses/categories`

Получить список категорий.

Доступ: все.

### POST `/api/Courses/categories`

Создать категорию.

Доступ: `admin`.

### PUT `/api/Courses/categories/{id}`

Обновить категорию.

Доступ: `admin`.

### DELETE `/api/Courses/categories/{id}`

Удалить категорию.

Доступ: `admin`.

## Sections

### GET `/api/courses/{courseId}/sections`

Получить разделы курса.

Доступ: все.

### GET `/api/courses/{courseId}/sections/{sectionId}`

Получить раздел курса по идентификатору.

Доступ: все.

### POST `/api/courses/{courseId}/sections`

Создать раздел курса.

Доступ: `teacher`, `admin`.

### PUT `/api/courses/{courseId}/sections/{sectionId}`

Обновить раздел курса.

Доступ: `teacher`, `admin`.

### DELETE `/api/courses/{courseId}/sections/{sectionId}`

Удалить раздел курса.

Доступ: `teacher`, `admin`.

## Lessons

### GET `/api/sections/{sectionId}/lessons`

Получить уроки раздела.

Доступ: все.

### GET `/api/sections/{sectionId}/lessons/{lessonId}`

Получить урок по идентификатору.

Доступ: все.

### POST `/api/sections/{sectionId}/lessons`

Создать урок.

Доступ: `teacher`, `admin`.

### PUT `/api/sections/{sectionId}/lessons/{lessonId}`

Обновить урок.

Доступ: `teacher`, `admin`.

### DELETE `/api/sections/{sectionId}/lessons/{lessonId}`

Удалить урок.

Доступ: `teacher`, `admin`.

## Enrollments

### GET `/api/Enrollments/my`

Получить мои записи на курсы.

Доступ: авторизованный пользователь.

### POST `/api/Enrollments`

Записаться на курс.

Доступ: авторизованный пользователь.

Пример тела запроса:

```json
{
  "courseId": 1
}
```

### DELETE `/api/Enrollments/{courseId}`

Отписаться от курса.

Доступ: авторизованный пользователь.

### GET `/api/Enrollments/course/{courseId}`

Получить студентов курса.

Доступ: `teacher`, `admin`.

## Progress

### POST `/api/Progress/update`

Обновить прогресс по уроку.

Доступ: авторизованный пользователь.

Пример тела запроса:

```json
{
  "lessonId": 1,
  "isCompleted": true,
  "watchTime": 600
}
```

### GET `/api/Progress/course/{courseId}`

Получить прогресс по курсу.

Доступ: авторизованный пользователь.

### GET `/api/Progress/lesson/{lessonId}`

Получить прогресс по уроку.

Доступ: авторизованный пользователь.

### GET `/api/Progress/my`

Получить прогресс по всем моим курсам.

Доступ: авторизованный пользователь.

## Reviews

### GET `/api/Reviews/course/{courseId}`

Получить одобренные отзывы курса.

Доступ: все.

### GET `/api/Reviews/course/{courseId}/moderation`

Получить отзывы курса для модерации.

Доступ: `admin`.

### GET `/api/Reviews/course/{courseId}/rating`

Получить рейтинг курса и распределение оценок.

Доступ: все.

### GET `/api/Reviews/my`

Получить мои отзывы.

Доступ: авторизованный пользователь.

### POST `/api/Reviews/course/{courseId}`

Создать отзыв.

Доступ: авторизованный пользователь.

Пример тела запроса:

```json
{
  "rating": 5,
  "comment": "Отличный курс"
}
```

### PUT `/api/Reviews/{id}`

Обновить отзыв.

Доступ: автор отзыва, `admin`.

### DELETE `/api/Reviews/{id}`

Удалить отзыв.

Доступ: автор отзыва, `admin`.

### PUT `/api/Reviews/{id}/approve`

Одобрить отзыв.

Доступ: `admin`.

## Files

### POST `/api/files/avatar`

Загрузить аватар пользователя.

Доступ: авторизованный пользователь.

Формат: `multipart/form-data`.

### POST `/api/files/lesson/{lessonId}`

Загрузить файл к уроку.

Доступ: `teacher`, `admin`.

Формат: `multipart/form-data`.

### GET `/api/files/download?fileUrl={fileUrl}`

Скачать файл.

Доступ: все.

## Коды ответов

- `200 OK` - запрос успешно выполнен.
- `201 Created` - ресурс создан.
- `400 Bad Request` - ошибка валидации или неверный запрос.
- `401 Unauthorized` - пользователь не авторизован.
- `403 Forbidden` - недостаточно прав.
- `404 Not Found` - ресурс не найден.
- `500 Internal Server Error` - внутренняя ошибка сервера.

## Тестовые данные SQLite

Локальная база создается автоматически при запуске API в development-режиме:

```text
OnlineCourses.API/online-courses-dev.db
```

Тестовые пользователи:

```text
teacher@local.dev / 123456
student@local.dev / 123456
student2@local.dev / 123456
admin@local.dev / 123456
```

Тестовые курсы:

- ASP.NET Core Web API Basics
- QA Automation for Online Courses API
- WPF Desktop Client Essentials

## Примечания для QA

- Проверять публичные GET-запросы без токена.
- Проверять защищенные POST/PUT/DELETE-запросы без токена, с ролью `student`, с ролью `teacher`, с ролью `admin`.
- Проверять уникальность email при регистрации.
- Проверять повторную запись студента на один курс.
- Проверять обновление прогресса только для курса, на который пользователь записан.
- Проверять валидацию размера и расширения файлов.
