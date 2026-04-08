namespace PracticeMonitoring.Api.Entities;

public class User
{
    public int Id { get; set; }

    // Для совместимости оставляем FullName (вывод)
    public string FullName { get; set; } = null!;

    // Разделённые поля
    public string Surname { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string? Patronymic { get; set; }

    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;

    // Роль
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    // Группа и курс (курс берётся из Group.Course)
    public int? GroupId { get; set; }
    public Group? Group { get; set; }
}
