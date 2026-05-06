# Performance tests

Для базовой проверки производительности используется k6.

Тест находится в файле:

```text
tests/performance/api-smoke.js
```

Он проверяет основные публичные GET endpoints:

- `GET /api/test/health`
- `GET /api/courses?pageNumber=1&pageSize=10`
- `GET /api/courses/categories`

## Запуск через Docker Compose

Сначала поднять проект:

```powershell
docker compose up -d --build
```

Запустить performance smoke test:

```powershell
docker compose -f docker-compose.yml -f docker-compose.performance.yml run --rm k6
```

## Настройка нагрузки

По умолчанию:

```text
10 virtual users
30 seconds
```

Можно изменить:

```powershell
$env:K6_VUS="20"
$env:K6_DURATION="1m"
docker compose -f docker-compose.yml -f docker-compose.performance.yml run --rm k6
```

## Критерии прохождения

Тест считается успешным, если:

- меньше 1% HTTP-ошибок;
- 95% запросов быстрее 1000 ms;
- больше 99% проверок успешны.

## Запуск локально через установленный k6

Если k6 установлен на машине:

```powershell
k6 run tests/performance/api-smoke.js
```

Для локального API:

```powershell
$env:API_BASE_URL="http://localhost:8080"
k6 run tests/performance/api-smoke.js
```
