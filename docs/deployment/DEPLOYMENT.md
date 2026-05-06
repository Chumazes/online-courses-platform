# Развертывание на выделенном сервере

Инструкция описывает запуск проекта через Docker Compose. В стек входят:

- `postgres` - база данных PostgreSQL;
- `api` - ASP.NET Core WebAPI;
- `web` - React/Vite приложение, собранное и запущенное через nginx.

## 1. Подготовить сервер

На сервере должны быть установлены:

- Docker;
- Docker Compose plugin;
- Git.

Проверка:

```bash
docker --version
docker compose version
git --version
```

## 2. Скопировать проект на сервер

```bash
git clone <URL_РЕПОЗИТОРИЯ> online-courses-platform
cd online-courses-platform
```

Если проект уже скопирован:

```bash
cd online-courses-platform
git pull
```

## 3. Создать файл окружения

Скопировать пример:

```bash
cp .env.example .env
```

Открыть файл:

```bash
nano .env
```

Обязательно заменить:

```text
POSTGRES_PASSWORD=change-this-postgres-password
JWT_SECRET=change-this-to-a-long-random-secret-at-least-32-characters
```

Для реального сервера также можно указать:

```text
API_BASE_URL=http://SERVER_IP:8080
WEB_PORT=3000
API_PORT=8080
```

## 4. Запустить проект

```bash
docker compose up -d --build
```

Проверить контейнеры:

```bash
docker compose ps
```

## 5. Проверить работу

API:

```text
http://SERVER_IP:8080/swagger
http://SERVER_IP:8080/api/test
```

Web:

```text
http://SERVER_IP:3000
```

Если запуск локальный:

```text
http://localhost:8080/swagger
http://localhost:8080/api/test
http://localhost:3000
```

## 6. Смотреть логи

Все сервисы:

```bash
docker compose logs -f
```

Только API:

```bash
docker compose logs -f api
```

Только React/nginx:

```bash
docker compose logs -f web
```

Только PostgreSQL:

```bash
docker compose logs -f postgres
```

Файловые логи API пишутся в:

```text
logs/api/
```

## 7. Остановить проект

Остановить контейнеры:

```bash
docker compose down
```

Остановить контейнеры и удалить volume базы данных:

```bash
docker compose down -v
```

Команду `docker compose down -v` использовать осторожно, потому что она удаляет данные PostgreSQL.

## 8. Обновить проект на сервере

```bash
git pull
docker compose up -d --build
docker compose ps
```

## 9. Частые проблемы

Если порт занят, поменять порт в `.env`:

```text
WEB_PORT=3001
API_PORT=8081
POSTGRES_PORT=5433
```

Если API не подключается к базе:

```bash
docker compose logs -f api
docker compose logs -f postgres
```

Если web открылся, но API-запросы не работают, проверить:

```bash
docker compose ps
docker compose logs -f web
docker compose logs -f api
```
