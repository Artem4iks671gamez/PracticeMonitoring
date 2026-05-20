# Финальная проверка проекта перед хостингом

Дата проверки: 20.05.2026  
Проект: `PracticeMonitoring`

## Краткий вывод

Проект в текущем состоянии **собирается, публикуется и выглядит технически близким к демонстрационной готовности**, но я бы не называл его полностью готовым к хостингу, пока не закрыты блокеры из раздела ниже. Основной функционал Web/API/Mobile присутствует: роли, практики, дневник, отчеты, PDF/DOCX, уведомления, чаты, профиль, мобильное приложение для студента.

Главные риски перед сервером: реальные секреты в `appsettings.json`, локальные URL вместо production URL, отсутствие сквозной проверки `IsActive`/`MustChangePassword` на API-запросах, нестойкое хранение загруженных файлов в `wwwroot` без volume/object storage, устаревший README и слабое покрытие тестами.

## Что проверено

- `dotnet build PracticeMonitoring.sln --no-restore` - успешно, 0 ошибок, 0 предупреждений.
- `dotnet test PracticeMonitoring.Tests\PracticeMonitoring.Tests.csproj --no-restore` - успешно, 4 теста пройдены.
- `dotnet publish PracticeMonitoring.Api\PracticeMonitoring.Api.csproj -c Release --no-restore -o artifacts\audit-publish-api` - успешно.
- `dotnet publish PracticeMonitoring.Web\PracticeMonitoring.Web.csproj -c Release --no-restore -o artifacts\audit-publish-web` - успешно.
- Проверены ключевые участки: API controllers, auth/profile/chat/student/supervisor flows, document generation, mobile pages, hosting configs, Dockerfiles, README.

## Блокеры перед хостингом

### 1. В репозитории лежат реальные локальные секреты

Файл: `PracticeMonitoring.Api/appsettings.json`

Сейчас там есть:

- строка подключения с паролем PostgreSQL;
- dev JWT key;
- Windows-пути к `pg_dump`/`pg_restore`.

Перед хостингом нужно:

- заменить `appsettings.json` на безопасные placeholder-значения или привести к виду `appsettings.example.json`;
- вынести реальные значения в переменные окружения сервера;
- сменить пароль БД и JWT key, если эти значения уже попадали в git/архив/демонстрационные материалы.

Минимум для production:

```text
ConnectionStrings__DefaultConnection=...
Jwt__Key=LONG_RANDOM_SECRET_AT_LEAST_32_CHARS
Jwt__Issuer=PracticeMonitoring.Api
Jwt__Audience=PracticeMonitoring.Client
DemoData__Seed=false
```

### 2. Web и Mobile по умолчанию смотрят на локальные адреса

Файлы:

- `PracticeMonitoring.Web/appsettings.json`
- `PracticeMonitoring.Mobile/Services/ApiClient.cs`

Web сейчас ожидает API по `https://localhost:7178/`. Mobile по умолчанию ожидает API/Web через `10.0.2.2` или `localhost`.

Перед хостингом:

- для Web задать `ApiSettings__BaseUrl=https://<api-domain>/`;
- для release APK либо поменять дефолтные URL на production, либо гарантировать, что пользователь сразу настраивает их в разделе подключения;
- проверить скачивание документов в Mobile через production `WebBaseUrl`.

### 3. JWT не проверяет актуальное состояние пользователя на каждом запросе

Login проверяет `IsActive`, Web дополнительно заставляет менять пароль через session middleware. Но API после выдачи JWT доверяет claims до конца срока жизни токена.

Риски:

- отключенный администратором пользователь может продолжать работать с уже выданным токеном до истечения JWT;
- пользователь с `MustChangePassword=true` может обращаться к API напрямую или через mobile flow после восстановления сессии;
- Mobile при автологине проверяет роль, но не принуждает к смене пароля по `MustChangePassword`.

Рекомендация: добавить проверку в `JwtBearerOptions.Events.OnTokenValidated` или глобальный authorization filter: пользователь существует, активен, роль актуальна; если `MustChangePassword`, разрешать только `/api/Auth/change-password`, `/api/Auth/me`, logout.

### 4. Загруженные файлы хранятся в файловой системе приложения

Файлы:

- `PracticeMonitoring.Api/Controllers/ProfileController.cs`
- `PracticeMonitoring.Web/Controllers/ProfileController.cs`
- `PracticeMonitoring.Web/Controllers/AdminController.cs`

Аватары кладутся в `wwwroot/uploads/avatars`. В контейнере или Render-подобном окружении это может пропасть при redeploy/restart, если нет постоянного volume. Кроме того, Web и API сохраняют аватары в разных приложениях и с разными URL-форматами: Web чаще относительный путь, API абсолютный URL.

Перед хостингом нужно выбрать один вариант:

- подключить persistent disk/volume для `wwwroot/uploads`;
- или вынести файлы в S3/Cloudinary/аналог;
- или хранить аватары в БД, если объем небольшой и это приемлемо для диплома.

### 5. README устарел и противоречит проекту

Файл: `README.md`

README пишет, что `PracticeMonitoring.Mobile` не содержит полноценного мобильного приложения и не подключен к решению. Фактически Mobile уже подключен к `.sln` и собирается.

Перед дипломом README надо обновить, иначе документация будет прямо спорить с демонстрацией.

## Важные замечания

### Безопасность и production-настройки

- `AllowedHosts` стоит `*` в API и Web. Для production лучше указать реальные домены.
- CORS в API разрешает только `https://localhost:7128`. Если планируются прямые browser-запросы к API с production Web-домена, добавить production origin. Если Web работает только server-to-server, это не критично, но конфиг все равно выглядит dev-only.
- В Web session cookie настроены `HttpOnly` и `IsEssential`, но нет явных `SecurePolicy=Always` и `SameSite`. Для HTTPS-hosting лучше задать явно.
- Нет rate limiting для login, forgot password, registration code. Email-коды имеют 5 попыток, но endpoint отправки кода можно дергать без ограничений.
- Email verification code хранится как SHA256 от `email:purpose:code`. При утечке БД 6-значный код можно перебрать офлайн. Лучше HMAC с серверным секретом или одноразовые случайные токены с rate limit.
- `EmailTestController` закрывается через `IsDevelopment()` и в production возвращает `404`, но перед хостингом его лучше удалить из production-сборки или дополнительно защитить, чтобы тестовый endpoint не светился в API.

### Загрузка файлов

- API для аватаров проверяет расширение и размер, но не проверяет реальный MIME/magic bytes.
- Web-профиль и Admin upload для аватаров сохраняют файл почти без server-side проверки размера/расширения/типа. `accept="image/*"` на клиенте не защита.
- `UpdateProfileRequest.AvatarUrl` позволяет клиенту передать произвольный URL аватара. Для диплома это терпимо, для сервера лучше принимать только результат upload endpoint или валидировать URL.
- Chat attachments и appendices хранятся в БД как byte array. Для учебного проекта нормально, для роста объема лучше файловое/object storage.

### Документы и PDF

- В Web уже есть защита от закрытия предпросмотра: `AbortController`, spinner, обработка отмены request, `499` для закрытого preview request.
- Dockerfile Web устанавливает LibreOffice Writer, значит PDF-конвертация в контейнере предусмотрена.
- На бесплатном hosting с cold start первый PDF может формироваться долго. Нужен понятный UX: spinner уже есть, но на демонстрации лучше заранее прогреть сервис.
- Если LibreOffice не найден, Mobile показывает понятное сообщение, что DOCX можно скачать, а PDF нет.

### Mobile

- Mobile собирается как `net9.0-android`. Если в дипломе заявляется кроссплатформенность, формулировку лучше ограничить Android-приложением.
- Раздел "Чаты" стал похож на мессенджер: есть диалоги, поиск, список контактов, карточки, bubbles. Но нет realtime/polling: новые сообщения появляются после открытия/обновления, не мгновенно.
- В Mobile можно прикреплять файл к сообщению, но вложения в уже полученных сообщениях показываются только как текст `Файл: ...`; скачать/открыть chat attachment из мобильного интерфейса пока нельзя.
- Student по умолчанию может начать новый чат только с руководителями; остальные пользователи доступны только через уже существующие диалоги. Если по предметной области студент должен писать работнику отдела, это нужно расширить в `ChatsController.GetAccessibleContactIdsAsync`.
- В профиле Mobile поля теперь закрыты до "Редактировать", есть предупреждение, аватарка, смена аватара и отдельный блок смены пароля. Осталось спорное место: в edit mode показывается ручное поле "Ссылка на аватар"; лучше убрать его из UI и оставить только выбор файла.
- Mobile dashboard логически готов к ситуации, когда практик больше двух: показывает агрегаты и первые 4 практики, остальные отправляет смотреть во вкладку "Практики".
- Не выполнена фактическая проверка на Android-эмуляторе скриншотами. Перед дипломом надо руками пройти маленький экран: login, dashboard, practices, documents, appendices share, chats, profile/avatar.

### Web UI

- Student document preview: spinner и отмена старого запроса реализованы.
- Вкладка документов в Mobile больше не содержит бесполезную кнопку "Проверить" и желтое окно про DOCX; в Web остались нормальные подсказки про DOCX-шаблон в редакторе отчета, это другое место.
- На Web профиль использует модалку и подтверждение изменений ФИО/email, но серверная проверка файла аватара слабая.
- Без реального браузерного smoke-test нельзя гарантировать отсутствие визуальных мелочей после всех изменений. Минимум перед сдачей: открыть Web на 1366px, 390px ширины, проверить Student modal, Admin modal, Supervisor dashboard.

## Предметная логика

Что выглядит правильно:

- Публичная регистрация всегда создает только Student и требует активную группу.
- Назначение студентов на практику проверяет специальность, архивность группы/специальности, дубли студентов.
- При смене специальности практики есть подтверждение удаления назначений.
- Студент может работать только со своими назначениями.
- Руководитель видит и проверяет только свои назначения.
- Дневник сбрасывает review-status после правки студентом, это логично.
- Готовность практики считает сведения организации, дневник, источники, приложения и разделы отчета.

Что стоит уточнить:

- Рабочие дни считаются как будни без учета праздников и индивидуального графика. Для диплома обычно нормально, но если в предметной области важны праздники, нужен календарь исключений.
- "Приложения обязательны" для готовности отчета. Если у некоторых практик приложений может не быть, правило надо сделать настраиваемым.
- Для отчета и дневника полная готовность требует заполнения всех рабочих дней. Это правильно для строгого контроля, но стоит проговорить на защите.
- Студентские чаты ограничены руководителями. Если отдел производственного обучения тоже должен быть доступен студенту, расширить список контактов.

## Тесты

Сейчас есть только 4 теста:

- demo seeder;
- JWT claims;
- password hash;
- temporary password generation.

Для дипломной уверенности этого мало. Минимум, который стоит добавить:

- Auth integration test: register/login/me/change-password.
- Student flow: список практик, сохранение организации, дневник, upload appendix.
- Supervisor flow: видит только свои назначения, review diary.
- DepartmentStaff flow: создание практики, назначение студентов, смена специальности с подтверждением.
- Document smoke test: DOCX generation без падения на demo data.
- Mobile API contract tests хотя бы на модели JSON, если UI-тесты не успевают.

## Чеклист перед сервером

1. Убрать реальные секреты из `appsettings.json`, заменить значения на placeholders.
2. Сменить DB password и JWT key.
3. Настроить production env vars для API и Web.
4. Поставить `DemoData__Seed=false`.
5. Настроить `ApiSettings__BaseUrl` для Web.
6. Настроить production API/Web URL в Mobile release.
7. Решить хранение `uploads/avatars`: volume или object storage.
8. Добавить проверку `IsActive` и `MustChangePassword` на API-запросах.
9. Добавить rate limiting для auth/email endpoints.
10. Ограничить `AllowedHosts`, CORS, cookie security.
11. Обновить README под фактический состав проекта.
12. Пройти ручной smoke-test Web + Android.

## Рекомендуемый порядок работ сегодня

Критично до хостинга:

1. Секреты, production URL, README.
2. `IsActive`/`MustChangePassword` enforcement в API.
3. Persistent storage для аватаров или хотя бы documented volume.
4. Mobile production URL и ручная проверка APK/эмулятора.

Важно до защиты:

1. Server-side validation аватаров в Web/Admin/API.
2. Rate limiting auth/email endpoints.
3. Скачивание chat attachments в Mobile.
4. 3-5 интеграционных тестов на основные роли.

Можно оставить как развитие:

1. Realtime chat через SignalR.
2. Object storage для всех файлов вместо БД.
3. Календарь праздников/индивидуальных рабочих дней.
4. Health checks, structured logs, metrics.

## Итоговая оценка готовности

Для локальной демонстрации и дипломной разработки проект выглядит готовым на уровне **примерно 80-85%**.  
Для размещения на сервере без неприятных сюрпризов нужно закрыть блокеры: секреты, production URL, JWT enforcement, хранение uploads, README и smoke-test на реальном Web/Mobile окружении.
