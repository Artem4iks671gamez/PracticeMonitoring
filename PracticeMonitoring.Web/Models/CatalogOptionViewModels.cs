namespace PracticeMonitoring.Web.Models.Catalog;

public class CatalogSpecialtyOptionViewModel
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
}

public class CatalogGroupOptionViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Course { get; set; }

    public int SpecialtyId { get; set; }
}
