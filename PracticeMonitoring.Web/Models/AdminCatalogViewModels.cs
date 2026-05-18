namespace PracticeMonitoring.Web.Models.Admin;

public class AdminCatalogViewModel
{
    public List<AdminSpecialtyCatalogViewModel> Specialties { get; set; } = new();

    public List<AdminSpecialtyCatalogViewModel> ActiveSpecialties => Specialties.Where(x => !x.IsArchived).ToList();

    public List<AdminSpecialtyCatalogViewModel> ArchivedSpecialties => Specialties.Where(x => x.IsArchived).ToList();

    public List<AdminGroupCatalogViewModel> ArchivedGroups => Specialties
        .SelectMany(x => x.Groups)
        .Where(x => x.IsArchived)
        .OrderBy(x => x.SpecialtyCode)
        .ThenBy(x => x.Name)
        .ToList();
}

public class AdminSpecialtyCatalogViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public bool IsArchived { get; set; }

    public int ActiveGroupsCount { get; set; }

    public int ArchivedGroupsCount { get; set; }

    public List<AdminGroupCatalogViewModel> Groups { get; set; } = new();
}

public class AdminGroupCatalogViewModel
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

public class AdminSaveSpecialtyViewModel
{
    public int? Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public class AdminSaveGroupViewModel
{
    public int? Id { get; set; }

    public int SpecialtyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Course { get; set; }
}
