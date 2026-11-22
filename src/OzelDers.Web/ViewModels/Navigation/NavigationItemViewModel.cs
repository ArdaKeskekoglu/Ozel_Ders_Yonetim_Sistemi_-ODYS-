namespace OzelDers.Web.ViewModels.Navigation;

/// <summary>
/// Masaüstü sol menü (Sidebar), mobil menü ve üst bar navigasyon öğelerini temsil eder.
/// </summary>
public class NavigationItemViewModel
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Controller { get; init; }
    public required string Action { get; init; }
    public required string IconText { get; init; }
    public string? BadgeText { get; init; }
    public bool IsActive { get; init; }
}
