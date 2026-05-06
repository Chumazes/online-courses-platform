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

Для смены адреса API внутри k6 используется отдельная переменная:

```powershell
$env:K6_API_BASE_URL="http://api:8080"
```

## Критерии прохождения

Тест считается успешным, если:

- меньше 1% HTTP-ошибок;
- 95% запросов быстрее 1000 ms;
- больше 99% проверок успешны.

## Локальный запуск через установленный k6

Если k6 установлен на машине:

```powershell
$env:K6_API_BASE_URL="http://localhost:8080"
k6 run tests/performance/api-smoke.js
```
