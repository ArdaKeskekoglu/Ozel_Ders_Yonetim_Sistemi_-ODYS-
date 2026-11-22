using OzelDers.Web.ViewModels.Navigation;

namespace OzelDers.Web.Services.Abstractions;

/// <summary>
/// Bilgi mimarisine (docs/04-ekranlar-ve-akislar.md) uygun olarak
/// ana sol menü ve sağ üst bar navigasyon öğelerini sağlar.
/// </summary>
public interface INavigationService
{
    IReadOnlyList<NavigationItemViewModel> GetSidebarItems(string? currentController, string? currentAction);
    IReadOnlyList<NavigationItemViewModel> GetTopbarItems(string? currentController, string? currentAction);
}
