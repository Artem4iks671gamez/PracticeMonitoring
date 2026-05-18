using System.ComponentModel.DataAnnotations;

namespace PracticeMonitoring.Api.Dtos;

public class AdminSpecialtyCatalogResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public int ActiveGroupsCount { get; set; }

    public int ArchivedGroupsCount { get; set; }

    public List<AdminGroupCatalogResponse> Groups { get; set; } = new();
}

public class AdminGroupCatalogResponse
{
    public int Id { get; set; }

    public int SpecialtyId { get; set; }

    public string SpecialtyCode { get; set; } = string.Empty;

    public string SpecialtyName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public int Course { get; set; }

    public bool IsArchived { get; set; }

    public int StudentCount { get; set; }
}

public class AdminSpecialtyUpsertRequest
{
    [Required(ErrorMessage = "Введите код специальности.")]
    [StringLength(50, ErrorMessage = "Код специальности не должен быть длиннее 50 символов.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите название специальности.")]
    [StringLength(200, ErrorMessage = "Название специальности не должно быть длиннее 200 символов.")]
    public string Name { get; set; } = string.Empty;
}

public class AdminGroupUpsertRequest
{
    [Required(ErrorMessage = "Выберите специальность.")]
    public int SpecialtyId { get; set; }

    [Required(ErrorMessage = "Введите название группы.")]
    [StringLength(100, ErrorMessage = "Название группы не должно быть длиннее 100 символов.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, 6, ErrorMessage = "Курс должен быть от 1 до 6.")]
    public int Course { get; set; }
}
