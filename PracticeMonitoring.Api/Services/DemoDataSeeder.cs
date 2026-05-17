using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PracticeMonitoring.Api.Data;
using PracticeMonitoring.Api.Entities;

namespace PracticeMonitoring.Api.Services;

public class DemoDataSeeder
{
    public const string DemoPassword = "Demo12345!";

    private readonly AppDbContext _context;
    private readonly PasswordService _passwordService;

    public DemoDataSeeder(AppDbContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var specialty = await EnsureSpecialtyAsync(cancellationToken);
        var group = await EnsureGroupAsync(specialty, cancellationToken);

        await EnsureUserAsync(
            "demo.admin@example.com",
            "Admin",
            "Смирнов",
            "Дмитрий",
            "Алексеевич",
            null,
            "/uploads/avatars/demo-admin.svg",
            cancellationToken);

        await EnsureUserAsync(
            "staff.demo@example.com",
            "DepartmentStaff",
            "Петрова",
            "Анна",
            "Сергеевна",
            null,
            "/uploads/avatars/demo-staff.svg",
            cancellationToken);

        var supervisor = await EnsureUserAsync(
            "supervisor.demo@example.com",
            "Supervisor",
            "Иванов",
            "Сергей",
            "Петрович",
            null,
            "/uploads/avatars/demo-supervisor.svg",
            cancellationToken);

        var student = await EnsureUserAsync(
            "student.demo@example.com",
            "Student",
            "Курбатов",
            "Артём",
            "Игоревич",
            group,
            "/uploads/avatars/demo-student.svg",
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        var practice = await EnsurePracticeAsync(specialty, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await EnsureAssignmentAsync(practice, student, supervisor, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<Specialty> EnsureSpecialtyAsync(CancellationToken cancellationToken)
    {
        const string code = "09.02.07";
        const string name = "Разработчик веб и мультимедийных приложений";

        var specialty = await _context.Specialties
            .FirstOrDefaultAsync(x => x.Code == code && x.Name == name, cancellationToken);

        if (specialty is not null)
            return specialty;

        specialty = new Specialty
        {
            Code = code,
            Name = name
        };

        _context.Specialties.Add(specialty);
        return specialty;
    }

    private async Task<Group> EnsureGroupAsync(Specialty specialty, CancellationToken cancellationToken)
    {
        const string name = "ВД50-1-22";

        var group = await _context.Groups
            .FirstOrDefaultAsync(x => x.Name == name && x.SpecialtyId == specialty.Id, cancellationToken);

        if (group is not null)
        {
            group.Course = 4;
            return group;
        }

        group = new Group
        {
            Name = name,
            Course = 4,
            Specialty = specialty
        };

        _context.Groups.Add(group);
        return group;
    }

    private async Task<User> EnsureUserAsync(
        string email,
        string roleName,
        string surname,
        string firstName,
        string? patronymic,
        Group? group,
        string avatarUrl,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles.FirstAsync(x => x.Name == roleName, cancellationToken);
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Email = normalizedEmail,
                Theme = "light"
            };

            _context.Users.Add(user);
        }

        user.Surname = surname;
        user.FirstName = firstName;
        user.Patronymic = patronymic;
        user.FullName = BuildFullName(surname, firstName, patronymic);
        user.RoleId = role.Id;
        user.Role = role;
        user.GroupId = group is { Id: > 0 } ? group.Id : null;
        user.Group = group;
        user.AvatarUrl = avatarUrl;
        user.IsActive = true;
        user.MustChangePassword = false;
        user.PasswordHash = _passwordService.HashPassword(user, DemoPassword);

        return user;
    }

    private async Task<ProductionPractice> EnsurePracticeAsync(Specialty specialty, CancellationToken cancellationToken)
    {
        const string practiceIndex = "ПП.04.01";
        const string practiceName = "Разработка модулей информационной системы";
        const string moduleCode = "ПМ.04";
        const string moduleName = "Разработка, администрирование и защита баз данных";

        var practice = await _context.ProductionPractices
            .Include(x => x.Competencies)
            .Include(x => x.GeneralCompetencies)
            .FirstOrDefaultAsync(x =>
                x.SpecialtyId == specialty.Id &&
                x.PracticeIndex == practiceIndex &&
                x.Name == practiceName,
                cancellationToken);

        if (practice is null)
        {
            practice = new ProductionPractice
            {
                Specialty = specialty,
                SpecialtyId = specialty.Id
            };

            _context.ProductionPractices.Add(practice);
        }
        else
        {
            _context.ProductionPracticeCompetencies.RemoveRange(practice.Competencies);
            _context.ProductionPracticeGeneralCompetencies.RemoveRange(practice.GeneralCompetencies);
        }

        practice.PracticeIndex = practiceIndex;
        practice.Name = practiceName;
        practice.ProfessionalModuleCode = moduleCode;
        practice.ProfessionalModuleName = moduleName;
        practice.Hours = 144;
        practice.StartDate = UtcDate(2026, 4, 6);
        practice.EndDate = UtcDate(2026, 5, 1);
        practice.Competencies = new List<ProductionPracticeCompetency>
        {
            new()
            {
                ProductionPractice = practice,
                CompetencyCode = "ПК 4.1",
                CompetencyDescription = "Формировать требования к информационной системе и разрабатывать программные модули.",
                WorkTypes = "Анализ требований, проектирование структуры данных, разработка серверной логики и пользовательских форм.",
                Hours = 36
            },
            new()
            {
                ProductionPractice = practice,
                CompetencyCode = "ПК 4.2",
                CompetencyDescription = "Выполнять интеграцию программных модулей и компонентов информационной системы.",
                WorkTypes = "Настройка взаимодействия Web-приложения с API, обработка данных, проверка пользовательских сценариев.",
                Hours = 36
            },
            new()
            {
                ProductionPractice = practice,
                CompetencyCode = "ПК 4.3",
                CompetencyDescription = "Проводить отладку, тестирование и сопровождение программного обеспечения.",
                WorkTypes = "Исправление ошибок, проверка валидации, анализ журналов, подготовка тестовых данных.",
                Hours = 36
            },
            new()
            {
                ProductionPractice = practice,
                CompetencyCode = "ПК 4.4",
                CompetencyDescription = "Оформлять техническую документацию и результаты выполненных работ.",
                WorkTypes = "Описание архитектуры, подготовка отчётных материалов, формирование документов по практике.",
                Hours = 36
            }
        };
        practice.GeneralCompetencies = new List<ProductionPracticeGeneralCompetency>
        {
            new()
            {
                ProductionPractice = practice,
                CompetencyCode = "ОК 01",
                CompetencyDescription = "Выбирать способы решения задач профессиональной деятельности применительно к различным контекстам.",
                SortOrder = 1
            },
            new()
            {
                ProductionPractice = practice,
                CompetencyCode = "ОК 02",
                CompetencyDescription = "Использовать современные средства поиска, анализа и интерпретации информации.",
                SortOrder = 2
            },
            new()
            {
                ProductionPractice = practice,
                CompetencyCode = "ОК 04",
                CompetencyDescription = "Эффективно взаимодействовать и работать в коллективе и команде.",
                SortOrder = 3
            },
            new()
            {
                ProductionPractice = practice,
                CompetencyCode = "ОК 09",
                CompetencyDescription = "Пользоваться профессиональной документацией на государственном и иностранном языках.",
                SortOrder = 4
            }
        };

        return practice;
    }

    private async Task EnsureAssignmentAsync(
        ProductionPractice practice,
        User student,
        User supervisor,
        CancellationToken cancellationToken)
    {
        var assignment = await _context.ProductionPracticeStudentAssignments
            .Include(x => x.DiaryEntries)
                .ThenInclude(x => x.Attachments)
            .Include(x => x.ReportItems)
            .Include(x => x.Sources)
            .Include(x => x.Appendices)
            .FirstOrDefaultAsync(x =>
                x.ProductionPracticeId == practice.Id &&
                x.StudentId == student.Id,
                cancellationToken);

        if (assignment is null)
        {
            assignment = new ProductionPracticeStudentAssignment
            {
                ProductionPracticeId = practice.Id,
                ProductionPractice = practice,
                StudentId = student.Id,
                Student = student
            };

            _context.ProductionPracticeStudentAssignments.Add(assignment);
        }
        else
        {
            _context.StudentPracticeDiaryAttachments.RemoveRange(assignment.DiaryEntries.SelectMany(x => x.Attachments));
            _context.StudentPracticeDiaryEntries.RemoveRange(assignment.DiaryEntries);
            _context.StudentPracticeReportItems.RemoveRange(assignment.ReportItems);
            _context.StudentPracticeSources.RemoveRange(assignment.Sources);
            _context.StudentPracticeAppendices.RemoveRange(assignment.Appendices);
        }

        assignment.SupervisorId = supervisor.Id;
        assignment.Supervisor = supervisor;
        assignment.AssignedAtUtc = UtcDateTime(2026, 4, 1, 9, 0);
        assignment.OrganizationName = "ООО \"Вектор Софт\"";
        assignment.OrganizationFullName = "Общество с ограниченной ответственностью \"Вектор Софт\"";
        assignment.OrganizationShortName = "ООО \"Вектор Софт\"";
        assignment.OrganizationAddress = "г. Москва, ул. Программная, д. 12, офис 405";
        assignment.OrganizationSupervisorFullName = "Соколова Марина Андреевна";
        assignment.OrganizationSupervisorPosition = "ведущий разработчик";
        assignment.OrganizationSupervisorPhone = "+7 (999) 100-20-30";
        assignment.OrganizationSupervisorEmail = "sokolova@example.com";
        assignment.PracticeTaskContent = "Изучить структуру Web-приложения, реализовать отдельные функции информационной системы, проверить корректность работы модулей и подготовить отчётные материалы.";
        assignment.StudentDuties = "Анализировать требования, разрабатывать программные модули, оформлять результаты работ в дневнике, соблюдать правила внутреннего распорядка и требования информационной безопасности.";
        assignment.ProvidedMaterialsDescription = "Рабочая станция, доступ к тестовой базе данных, репозиторий проекта, макеты экранов, техническое задание и внутренняя документация организации.";
        assignment.WorkScheduleDescription = "Пятидневная рабочая неделя, с 09:00 до 16:00, перерыв с 13:00 до 13:30.";
        assignment.IntroductionMainGoal = "Закрепление профессиональных навыков разработки и сопровождения информационной системы на базе Web-технологий.";
        assignment.StudentDetailsUpdatedAtUtc = UtcDateTime(2026, 4, 2, 10, 30);
        assignment.DiaryEntries = BuildDiaryEntries(assignment, supervisor);
        assignment.ReportItems = BuildReportItems(assignment);
        assignment.Sources = BuildSources(assignment);
        assignment.Appendices = BuildAppendices(assignment);
    }

    private static List<StudentPracticeDiaryEntry> BuildDiaryEntries(
        ProductionPracticeStudentAssignment assignment,
        User supervisor)
    {
        var descriptions = new[]
        {
            "Ознакомление с организацией, рабочим местом и требованиями охраны труда.",
            "Изучение технического задания и структуры существующей информационной системы.",
            "Анализ ролей пользователей и основных сценариев работы приложения.",
            "Проектирование структуры данных для модуля производственной практики.",
            "Настройка локального окружения разработки и подключение к тестовой базе данных.",
            "Разработка серверной логики для обработки данных студентов и практик.",
            "Реализация клиентских форм для просмотра и редактирования сведений о практике.",
            "Настройка валидации обязательных полей и сообщений об ошибках.",
            "Разработка функционала заполнения дневника практики по дням.",
            "Проверка сохранения подробных отчётов и вложений к дневнику.",
            "Интеграция функций генерации документов по данным практики.",
            "Тестирование формирования отчёта и дневника практики.",
            "Исправление ошибок отображения и обработки пользовательских данных.",
            "Оптимизация пользовательского интерфейса модальных окон и таблиц.",
            "Проверка сценариев руководителя практики по оцениванию дневника.",
            "Подготовка справочных данных для демонстрации работы системы.",
            "Анализ журналов действий и уведомлений пользователей.",
            "Подготовка описания реализованных модулей и структуры базы данных.",
            "Финальное тестирование пользовательских сценариев.",
            "Оформление итоговых материалов и передача результатов руководителю."
        };

        return EnumerateWorkDays(UtcDate(2026, 4, 6), UtcDate(2026, 5, 1))
            .Select((date, index) => new StudentPracticeDiaryEntry
            {
                Assignment = assignment,
                WorkDate = date,
                ShortDescription = descriptions[index],
                DetailedReport = BuildDetailedReportJson(descriptions[index], index + 1),
                IsReviewed = true,
                SupervisorGrade = index % 5 == 0 ? 4 : 5,
                SupervisorComment = index % 5 == 0
                    ? "Работа выполнена, есть небольшие замечания по детализации описания."
                    : "Работа выполнена качественно, результат принят.",
                ReviewedBySupervisorId = supervisor.Id,
                ReviewedBySupervisor = supervisor,
                ReviewedAtUtc = date.Date.AddHours(14),
                CreatedAtUtc = date.Date.AddHours(9),
                UpdatedAtUtc = date.Date.AddHours(14)
            })
            .ToList();
    }

    private static List<StudentPracticeReportItem> BuildReportItems(ProductionPracticeStudentAssignment assignment)
    {
        var items = new List<StudentPracticeReportItem>();

        AddReportItem(items, assignment, "IntroductionWorkType", "Анализ требований", "Изучение предметной области и пользовательских сценариев.");
        AddReportItem(items, assignment, "IntroductionWorkType", "Разработка модулей", "Реализация серверной и клиентской логики информационной системы.");
        AddReportItem(items, assignment, "IntroductionWorkType", "Тестирование", "Проверка корректности сохранения данных, документов и ролей пользователей.");
        AddReportItem(items, assignment, "IntroductionSoftwareTechnology", "ASP.NET Core", "Разработка Web-приложения и REST API.");
        AddReportItem(items, assignment, "IntroductionSoftwareTechnology", "Entity Framework Core", "Работа с базой данных и миграциями.");
        AddReportItem(items, assignment, "IntroductionSoftwareTechnology", "PostgreSQL", "Хранение данных системы мониторинга практики.");
        AddReportItem(items, assignment, "IntroductionSoftwareTechnology", "JavaScript", "Реализация интерактивных экранов и модальных окон.");
        AddReportItem(items, assignment, "SoftwareTool", "Visual Studio 2022", "Разработка и отладка проекта.");
        AddReportItem(items, assignment, "SoftwareTool", "Render", "Размещение Web-приложения на бесплатном облачном хостинге.");

        AddReportItem(items, assignment, "TechnicalTool", "Компьютер", "Рабочая станция разработчика");
        AddReportItem(items, assignment, "TechnicalTool", "Размер экрана", "15.6 дюйма");
        AddReportItem(items, assignment, "TechnicalTool", "Разрешение экрана", "1920x1080");
        AddReportItem(items, assignment, "TechnicalTool", "Процессор", "Intel Core i5");
        AddReportItem(items, assignment, "TechnicalTool", "Количество ядер процессора", "6");
        AddReportItem(items, assignment, "TechnicalTool", "Оперативная память", "16 ГБ");
        AddReportItem(items, assignment, "TechnicalTool", "Тип видеокарты", "интегрированная");
        AddReportItem(items, assignment, "TechnicalTool", "Видеокарта", "Intel UHD Graphics");
        AddReportItem(items, assignment, "TechnicalTool", "Конфигурация накопителей", "SSD 512 ГБ");
        AddReportItem(items, assignment, "TechnicalTool", "Общий объем всех накопителей", "512 ГБ");
        AddReportItem(items, assignment, "TechnicalTool", "Операционная система", "Windows 11");

        return items;
    }

    private static List<StudentPracticeSource> BuildSources(ProductionPracticeStudentAssignment assignment)
    {
        return new List<StudentPracticeSource>
        {
            new()
            {
                Assignment = assignment,
                Title = "Документация Microsoft по ASP.NET Core",
                Url = "https://learn.microsoft.com/aspnet/core",
                Description = "Использовалась при настройке Web-приложения, маршрутизации и контроллеров.",
                SortOrder = 1
            },
            new()
            {
                Assignment = assignment,
                Title = "Документация Entity Framework Core",
                Url = "https://learn.microsoft.com/ef/core",
                Description = "Использовалась при работе с моделями данных, миграциями и запросами.",
                SortOrder = 2
            },
            new()
            {
                Assignment = assignment,
                Title = "Внутреннее техническое задание организации",
                Description = "Содержит требования к модулю мониторинга производственной практики.",
                SortOrder = 3
            }
        };
    }

    private static List<StudentPracticeAppendix> BuildAppendices(ProductionPracticeStudentAssignment assignment)
    {
        const string content = """
            Демонстрационное приложение к отчёту.

            Содержит краткое описание структуры реализованного модуля:
            - API для работы с пользователями, практиками и дневником;
            - Web-интерфейс для ролей студента, руководителя и работника отдела;
            - генерация отчётных документов по данным практики.
            """;

        var bytes = Encoding.UTF8.GetBytes(content);
        return new List<StudentPracticeAppendix>
        {
            new()
            {
                Assignment = assignment,
                Title = "Описание реализованного модуля",
                Description = "Краткое текстовое приложение для демонстрационного отчёта.",
                FileName = "demo-module-description.txt",
                ContentType = "text/plain",
                SizeBytes = bytes.Length,
                Content = bytes,
                CreatedAtUtc = UtcDateTime(2026, 5, 1, 15, 0)
            }
        };
    }

    private static void AddReportItem(
        List<StudentPracticeReportItem> items,
        ProductionPracticeStudentAssignment assignment,
        string category,
        string name,
        string description)
    {
        items.Add(new StudentPracticeReportItem
        {
            Assignment = assignment,
            Category = category,
            Name = name,
            Description = description,
            SortOrder = items.Count(x => x.Category == category) + 1
        });
    }

    private static string BuildDetailedReportJson(string shortDescription, int dayNumber)
    {
        var document = new
        {
            version = 3,
            type = "practice-day-report",
            blocks = new object[]
            {
                new
                {
                    id = $"day-{dayNumber}-summary",
                    type = "text",
                    content = $"{shortDescription} В ходе работы были изучены исходные материалы, выполнены практические действия по заданию и зафиксированы результаты для отчётной документации.",
                    mode = "paragraph"
                }
            },
            attachments = Array.Empty<object>()
        };

        return JsonSerializer.Serialize(document);
    }

    private static IEnumerable<DateTime> EnumerateWorkDays(DateTime startDate, DateTime endDate)
    {
        for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                continue;

            yield return DateTime.SpecifyKind(date, DateTimeKind.Utc);
        }
    }

    private static DateTime UtcDate(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    private static DateTime UtcDateTime(int year, int month, int day, int hour, int minute)
        => new(year, month, day, hour, minute, 0, DateTimeKind.Utc);

    private static string BuildFullName(string surname, string firstName, string? patronymic)
        => string.IsNullOrWhiteSpace(patronymic)
            ? $"{surname} {firstName}"
            : $"{surname} {firstName} {patronymic}";
}
