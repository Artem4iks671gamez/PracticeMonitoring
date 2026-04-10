using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Web.Models.Admin;

public class AdminSaveUserViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Введите фамилию")]
    public string Surname { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите имя")]
    public string FirstName { get; set; } = string.Empty;

    public string? Patronymic { get; set; }

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Введите корректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Укажите роль")]
    public string Role { get; set; } = string.Empty;

    public int? GroupId { get; set; }

    public bool IsActive { get; set; } = true;

    public bool RemoveAvatar { get; set; }

    public string? CurrentAvatarUrl { get; set; }

    public IFormFile? AvatarFile { get; set; }

    public string? Password { get; set; }

    public bool IsCreateMode { get; set; }
}