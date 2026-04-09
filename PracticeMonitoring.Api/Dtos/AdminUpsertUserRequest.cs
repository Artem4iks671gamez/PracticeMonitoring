using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Api.Dtos;

public class AdminUpsertUserRequest
{
    [Required(ErrorMessage = "Введите фамилию.")]
    public string Surname { get; set; } = null!;

    [Required(ErrorMessage = "Введите имя.")]
    public string FirstName { get; set; } = null!;

    public string? Patronymic { get; set; }

    [Required(ErrorMessage = "Введите email.")]
    [EmailAddress(ErrorMessage = "Некорректный формат email.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Укажите роль.")]
    public string Role { get; set; } = null!;

    public int? GroupId { get; set; }

    public string? AvatarUrl { get; set; }

    public bool RemoveAvatar { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Password { get; set; }
}