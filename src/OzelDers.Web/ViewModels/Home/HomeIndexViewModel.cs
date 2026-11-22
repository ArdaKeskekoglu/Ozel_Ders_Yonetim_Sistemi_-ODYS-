namespace OzelDers.Web.ViewModels.Home;

/// <summary>
/// Ana sayfa (Home/Index) görünümü için güçlü tipli ViewModel.
/// </summary>
public class HomeIndexViewModel
{
    public required string ApplicationName { get; init; }
    public required string ShortCode { get; init; }
    public required string Description { get; init; }

    public string FullTitle => $"{ApplicationName} ({ShortCode})";
}
