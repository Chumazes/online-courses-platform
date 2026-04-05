# API Документация: Платформа онлайн-курсов

## Общая информация

Базовый URL: http://localhost:5064/api

Аутентификация: Authorization: Bearer {your_access_token}

Сроки действия токенов: Access Token - 15 минут, Refresh Token - 7 дней

Роли пользователей: student (просмотр курсов, запись, отзывы, прогресс), teacher (всё из student + создание/редактирование курсов, разделов, уроков), admin (всё из teacher + модерация отзывов, управление пользователями)

## Все эндпоинты

POST /api/Auth/register - Регистрация - Доступ: Все
POST /api/Auth/login - Вход - Доступ: Все
POST /api/Auth/refresh - Обновить токен - Доступ: Все
POST /api/Auth/logout - Выход - Доступ: Авторизованные
POST /api/Auth/logout-all - Выход со всех устройств - Доступ: Авторизованные
GET /api/Auth/me - Мой профиль - Доступ: Авторизованные

GET /api/Courses - Все курсы (с кэшем) - Доступ: Все
GET /api/Courses/{id} - Курс по ID (с кэшем) - Доступ: Все
POST /api/Courses - Создать курс - Доступ: Teacher/Admin
PUT /api/Courses/{id} - Обновить курс - Доступ: Teacher/Admin
DELETE /api/Courses/{id} - Удалить курс - Доступ: Teacher/Admin
GET /api/Courses/my - Мои курсы - Доступ: Teacher/Admin

GET /api/Enrollments/my - Мои курсы - Доступ: Авторизованные
POST /api/Enrollments - Записаться на курс - Доступ: Авторизованные
DELETE /api/Enrollments/{courseId} - Отписаться от курса - Доступ: Авторизованные
GET /api/Enrollments/course/{courseId} - Студенты курса - Доступ: Teacher/Admin

GET /api/courses/{courseId}/sections - Разделы курса - Доступ: Все
GET /api/courses/{courseId}/sections/{sectionId} - Раздел по ID - Доступ: Все
POST /api/courses/{courseId}/sections - Создать раздел - Доступ: Teacher/Admin
PUT /api/courses/{courseId}/sections/{sectionId} - Обновить раздел - Доступ: Teacher/Admin
DELETE /api/courses/{courseId}/sections/{sectionId} - Удалить раздел - Доступ: Teacher/Admin

GET /api/sections/{sectionId}/lessons - Уроки раздела - Доступ: Все
GET /api/sections/{sectionId}/lessons/{lessonId} - Урок по ID - Доступ: Все
POST /api/sections/{sectionId}/lessons - Создать урок - Доступ: Teacher/Admin
PUT /api/sections/{sectionId}/lessons/{lessonId} - Обновить урок - Доступ: Teacher/Admin
DELETE /api/sections/{sectionId}/lessons/{lessonId} - Удалить урок - Доступ: Teacher/Admin

POST /api/Progress/update - Обновить прогресс урока - Доступ: Авторизованные
GET /api/Progress/course/{courseId} - Прогресс по курсу - Доступ: Авторизованные
GET /api/Progress/lesson/{lessonId} - Прогресс по уроку - Доступ: Авторизованные
GET /api/Progress/my - Прогресс по всем курсам - Доступ: Авторизованные

GET /api/Reviews/course/{courseId} - Отзывы курса - Доступ: Все
GET /api/Reviews/course/{courseId}/rating - Рейтинг курса (средний + распределение) - Доступ: Все
GET /api/Reviews/my - Мои отзывы - Доступ: Авторизованные
POST /api/Reviews/course/{courseId} - Создать отзыв (требует модерации) - Доступ: Авторизованные
PUT /api/Reviews/{id} - Обновить отзыв - Доступ: Автор/Admin
DELETE /api/Reviews/{id} - Удалить отзыв - Доступ: Автор/Admin
PUT /api/Reviews/{id}/approve - Одобрить отзыв - Доступ: Admin

## Примеры запросов и ответов

Регистрация: POST /api/Auth/register Body: {"email":"user@test.com","password":"123456","fullName":"Иван Иванов"} Ответ: {"message":"User registered successfully"}

Вход: POST /api/Auth/login Body: {"email":"user@test.com","password":"123456"} Ответ: {"accessToken":"eyJhbGciOiJIUzI1NiIs...","refreshToken":"sRcIzpRxIZ861H3qQtMYUx...","userId":1,"email":"user@test.com","fullName":"Иван Иванов","role":"student","expiresAt":"2026-04-05T15:56:17Z"}

Создание курса: POST /api/Courses Body: {"title":"Python для начинающих","description":"Полный курс по Python с нуля","price":4990,"level":"beginner","categoryId":1,"coverImageUrl":"https://example.com/cover.jpg"} Ответ: {"courseId":1,"title":"Python для начинающих","status":"draft","authorId":1,"createdAt":"2026-04-05T15:30:00Z"}

Все курсы: GET /api/Courses Ответ: [{"courseId":1,"title":"Python для начинающих","price":4990,"avgRating":4.8,"authorName":"Иван Иванов","totalStudents":25}]

Создание отзыва: POST /api/Reviews/course/1 Body: {"rating":5,"comment":"Отличный курс! Всё понятно и доступно."} Ответ: {"message":"Review submitted successfully. Waiting for moderation."}

Рейтинг курса: GET /api/Reviews/course/1/rating Ответ: {"courseId":1,"courseTitle":"Python для начинающих","averageRating":4.8,"totalReviews":25,"ratingDistribution":{"1":0,"2":0,"3":1,"4":3,"5":21}}

Запись на курс: POST /api/Enrollments Body: {"courseId":1} Ответ: {"enrollmentId":1,"courseId":1,"courseTitle":"Python для начинающих","status":"active","overallProgress":0}

Обновление прогресса: POST /api/Progress/update Body: {"lessonId":1,"isCompleted":true,"watchTime":600} Ответ: {"message":"Progress updated successfully"}

Прогресс по курсу: GET /api/Progress/course/1 Ответ: {"courseId":1,"courseTitle":"Python для начинающих","totalLessons":25,"completedLessons":10,"overallProgress":40,"status":"active"}

## Как получить роль teacher или admin (временно через базу данных)

Подключитесь к PostgreSQL (через pgAdmin) и выполните: UPDATE "Users" SET "Role" = 'teacher' WHERE "Email" = 'user@test.com'; UPDATE "Users" SET "Role" = 'admin' WHERE "Email" = 'admin@test.com';

## Swagger документация

После запуска API Swagger доступен по адресу: http://localhost:5064/swagger

## Коды ответов HTTP

200 - Успешно
201 - Создано
400 - Неверный запрос (ошибка валидации)
401 - Не авторизован (требуется токен)
403 - Доступ запрещён (недостаточно прав)
404 - Не найдено
500 - Внутренняя ошибка сервера

## Примечания

- Все GET запросы к курсам используют кэширование (5 минут). После создания, обновления или удаления курса кэш автоматически очищается.
- Отзывы требуют модерации, поле IsApproved = false по умолчанию. Администратор может одобрить отзыв через PUT /api/Reviews/{id}/approve.
- Прогресс по курсу рассчитывается автоматически на основе завершённых уроков.
- При обновлении или удалении отзыва рейтинг курса пересчитывается автоматически.