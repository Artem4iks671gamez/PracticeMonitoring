# Practice Monitoring

Система мониторинга производственной практики для студентов, руководителей практики, работников отдела производственного обучения и администратора.

Проект состоит из двух основных приложений:

- `PracticeMonitoring.Api` - ASP.NET Core Web API, PostgreSQL, JWT, миграции EF Core.
- `PracticeMonitoring.Web` - ASP.NET Core MVC Web-интерфейс, работающий через API.

Папка `PracticeMonitoring.Mobile` сейчас не содержит полноценного исходного мобильного приложения и не подключена к решению. Для диплома её лучше указывать как направление развития, если мобильная версия не будет реализована отдельно.

## Возможности

- регистрация и вход пользователей;
- роли `Admin`, `Student`, `Supervisor`, `DepartmentStaff`;
- управление пользователями, группами и специальностями;
- создание производственных практик, компетенций и назначений студентов;
- заполнение студентом сведений об организации, дневника и отчёта;
- проверка дневника руководителем практики;
- генерация отчёта, дневника практики и аттестационного листа;
- предпросмотр документов в PDF;
- уведомления, чат, журнал действий;
- резервное копирование и восстановление БД через административный модуль.

## Требования

- .NET SDK 9;
- PostgreSQL 15 или новее;
- Visual Studio 2022 / Rider / VS Code;
- для отправки писем - SMTP-аккаунт.

## Локальный запуск

1. Создайте базу данных PostgreSQL:

```powershell
createdb practice_monitoring_db
```

2. Настройте секреты API:

```powershell
cd PracticeMonitoring.Api

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=practice_monitoring_db;Username=postgres;Password=YOUR_PASSWORD"
dotnet user-secrets set "Jwt:Key" "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_32_CHARS_MIN"
dotnet user-secrets set "Jwt:Issuer" "PracticeMonitoring.Api"
dotnet user-secrets set "Jwt:Audience" "PracticeMonitoring.Client"
```

3. При необходимости настройте SMTP:

```powershell
dotnet user-secrets set "Smtp:Host" "smtp.gmail.com"
dotnet user-secrets set "Smtp:Port" "587"
dotnet user-secrets set "Smtp:UseSsl" "false"
dotnet user-secrets set "Smtp:Username" "YOUR_SMTP_LOGIN"
dotnet user-secrets set "Smtp:Password" "YOUR_SMTP_PASSWORD"
dotnet user-secrets set "Smtp:FromEmail" "YOUR_EMAIL"
dotnet user-secrets set "Smtp:FromName" "Practice Monitoring"
```

4. Запустите API:

```powershell
dotnet run --project PracticeMonitoring.Api
```

В режиме Development миграции применяются автоматически.

5. Запустите Web-приложение во втором терминале:

```powershell
dotnet run --project PracticeMonitoring.Web
```

По умолчанию Web ожидает API по адресу `https://localhost:7178/`. Настройка находится в `PracticeMonitoring.Web/appsettings.json`.

## Демо-данные

В `PracticeMonitoring.Api/appsettings.Development.json` включена настройка:

```json
{
  "DemoData": {
    "Seed": true
  }
}
```

При запуске API в локальной разработке сидер создаёт демонстрационные данные:

- специальность `09.02.07 Разработчик веб и мультимедийных приложений`;
- группу `ВД50-1-22`, 4 курс;
- практику `ПП.04.01 Разработка модулей информационной системы`;
- компетенции, назначение студента, сведения об организации;
- заполненный дневник по рабочим дням;
- проверку дневника руководителем;
- элементы отчёта, источники и приложение;
- демо-пользователей с аватарками.

Демо-аккаунты:

| Роль | Email | Пароль |
| --- | --- | --- |
| Администратор | `demo.admin@example.com` | `Demo12345!` |
| Работник отдела | `staff.demo@example.com` | `Demo12345!` |
| Руководитель | `supervisor.demo@example.com` | `Demo12345!` |
| Студент | `student.demo@example.com` | `Demo12345!` |

Сидер идемпотентный: при повторном запуске он обновляет демо-записи, а не создаёт бесконечные дубли.

## Render

Проект можно размещать на Render как два отдельных Web Service:

- API: сборка и запуск `PracticeMonitoring.Api`;
- Web: сборка и запуск `PracticeMonitoring.Web`.

Для API нужны переменные окружения:

```text
ConnectionStrings__DefaultConnection=Host=...;Port=5432;Database=...;Username=...;Password=...
Jwt__Key=LONG_RANDOM_SECRET_32_CHARS_MIN
Jwt__Issuer=PracticeMonitoring.Api
Jwt__Audience=PracticeMonitoring.Client
Database__AutoMigrate=true
DemoData__Seed=false
```

Для Web нужна переменная:

```text
ApiSettings__BaseUrl=https://YOUR_API_SERVICE.onrender.com/
```

На бесплатном тарифе Render сервисы могут засыпать после простоя. Первый запрос после паузы будет выполняться дольше.

## Проверка

Сборка решения:

```powershell
dotnet build PracticeMonitoring.sln
```

Запуск тестов:

```powershell
dotnet test PracticeMonitoring.sln
```

Рекомендуемый сценарий демонстрации:

1. Войти как работник отдела и показать созданную практику.
2. Открыть назначение студента и руководителя.
3. Войти как студент и показать сведения о практике, дневник, отчёт и генерацию документов.
4. Войти как руководитель и показать проверку дневника.
5. Войти как администратор и показать пользователей, логи и резервное копирование.

## Статус мобильного приложения

Мобильный проект сейчас не является готовой частью системы. Для дипломного уровня есть два реалистичных варианта:

- оставить мобильную версию как перспективу развития и защищать Web + API;
- сделать минимальное MAUI-приложение для студента: вход, список практик, просмотр дневника, заполнение краткой записи за день и просмотр уведомлений.

Второй вариант потребует отдельного времени на UI, хранение токена, API-клиент и тестирование на Android.
