# Размещение PracticeMonitoring на VPS

Этот вариант рассчитан на обычный VPS/VDS с Ubuntu и Docker. На сервере поднимаются:

- `PostgreSQL`;
- `PracticeMonitoring.Api`;
- `PracticeMonitoring.Web`;
- `Caddy` как reverse proxy с автоматическим HTTPS.

## 1. DNS

Лучше использовать два домена или поддомена:

```text
practice.example.com      -> IP сервера
api.practice.example.com  -> IP сервера
```

Обе A-записи должны указывать на публичный IP VPS. Без домена Caddy не сможет автоматически получить HTTPS-сертификат.

## 2. Подготовка сервера

Подключиться по SSH:

```bash
ssh root@SERVER_IP
```

Установить Docker:

```bash
apt update
apt install -y ca-certificates curl git
curl -fsSL https://get.docker.com | sh
```

Открыть firewall, если используется `ufw`:

```bash
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
```

## 3. Загрузка проекта

Клонировать проект или загрузить папку проекта на сервер:

```bash
git clone <YOUR_REPOSITORY_URL> /opt/practice-monitoring
cd /opt/practice-monitoring
```

Если проекта нет в git, можно загрузить архивом/SFTP в `/opt/practice-monitoring`.

## 4. Настройка окружения

Создать production env:

```bash
cp deploy/env.example deploy/.env
nano deploy/.env
```

Обязательно заменить:

```text
WEB_DOMAIN=practice.example.com
API_DOMAIN=api.practice.example.com
POSTGRES_PASSWORD=...
JWT_KEY=...
SMTP_USERNAME=...
SMTP_PASSWORD=...
SMTP_FROM_EMAIL=...
```

JWT key можно сгенерировать так:

```bash
openssl rand -base64 48
```

Если пароль содержит символ `$`, в `.env` для Docker Compose его лучше экранировать как `$$`.

## 5. Запуск

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d --build
```

Проверить состояние:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml ps
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs -f api
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs -f web
docker compose --env-file deploy/.env -f deploy/docker-compose.yml logs -f caddy
```

Первый запуск API применит EF migrations, потому что `DATABASE_AUTO_MIGRATE=true`.

## 6. Проверка после запуска

Открыть:

```text
https://practice.example.com
https://api.practice.example.com
```

Для API корневой адрес может вернуть 404, это нормально. Проверять лучше конкретные endpoint'ы или вход через Web.

Минимальный smoke-test:

1. Открыть Web login.
2. Зарегистрировать студента или войти demo/user аккаунтом.
3. Проверить список практик.
4. Открыть генерацию DOCX/PDF.
5. Проверить загрузку аватарки.
6. Проверить чат и уведомления.
7. В Mobile в настройках подключения указать `https://api.practice.example.com/` и `https://practice.example.com/`.

## 7. Обновление проекта

```bash
cd /opt/practice-monitoring
git pull
docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d --build
```

## 8. Резервная копия БД

Через Docker Compose:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.yml exec db pg_dump -U practice_monitoring -d practice_monitoring_db -Fc -f /tmp/practice-monitoring.dump
docker compose --env-file deploy/.env -f deploy/docker-compose.yml cp db:/tmp/practice-monitoring.dump ./practice-monitoring.dump
```

Или через админский раздел приложения, если SMTP и авторизация уже настроены.

## 9. Если домена пока нет

Временный вариант для проверки по IP уже вынесен в отдельный compose-файл:

```bash
docker compose --env-file deploy/.env -f deploy/docker-compose.ip.yml up -d --build
```

Открывать:

```text
http://SERVER_IP
http://SERVER_IP:7178
```

Для финального хостинга лучше все равно подключить домен и HTTPS.
