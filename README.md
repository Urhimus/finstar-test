
# Task Management Service

## Описание

Тестовое задание для Finstar.

## Быстрый старт

### 1. Предварительные требования

* .NET SDK 8.0
* RabbitMQ
* PostgreSQL
Либо
* Docker 24+ и docker compose v3

### 2. Клонирование репозитория

```bash
git clone https://github.com/Urhimus/finstar-test.git
cd finstar-test
```

### 3. Запуск через Docker Compose

```bash
docker compose up --build -d
```
Открыть сваггер http://localhost:62352/swagger/index.html

### 4. Локальный запуск без Docker

1. Настроить ConnectionString в appsettings.json у TaskManagement.API
1. Настроить RabbitMQ конфигурацию в appsettings.json у TaskManagement.API и TaskManagement.Consumer
2. Запустить start.ps1


# Табличная функция SQL

```sql
CREATE OR REPLACE FUNCTION get_client_daily_payments(
    p_client_id  bigint,
    p_start_date date,
    p_end_date   date
)
RETURNS TABLE(dt date, amount numeric)  
LANGUAGE sql STABLE
AS $$
WITH cal AS (                                   
     SELECT gs::date AS dt
     FROM generate_series(p_start_date,
                          p_end_date,
                          interval '1 day') AS gs
),
aggr AS (                                     
     SELECT c.dt::date AS dt,
            SUM(c.amount)   AS day_sum
     FROM   clientpayments c
     WHERE  c.clientid = p_client_id
       AND  c.dt::date BETWEEN p_start_date AND p_end_date
     GROUP  BY c.dt::date
)
SELECT
       cal.dt,
       COALESCE(aggr.day_sum, 0) AS amount
FROM   cal
LEFT   JOIN aggr ON aggr.dt = cal.dt       
ORDER  BY cal.dt;
$$;
```