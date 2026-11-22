using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Navigation;

namespace OzelDers.Web.Services;

/// <summary>
/// docs/04-ekranlar-ve-akislar.md belgesindeki bilgi mimarisini uygular:
/// Sol Menü: Ana Sayfa · Öğrenciler · Ders Türleri · Ders Programı · Ders Geçmişi · Ödemeler · Raporlar
/// Sağ Üst: Bildirim · Profil · Ayarlar
/// </summary>
public class NavigationService : INavigationService
{
    public IReadOnlyList<NavigationItemViewModel> GetSidebarItems(string? currentController, string? currentAction)
    {
        var items = new (string Key, string Title, string Controller, string Action, string IconText)[]
        {
            ("dashboard", "Ana Sayfa", "Home", "Index", "⌂"),
            ("students", "Öğrenciler", "Students", "Index", "👥"),
            ("subjects", "Ders Türleri", "Subjects", "Index", "📘"),
            ("schedule", "Ders Programı", "Schedule", "Index", "📅"),
            ("lessons", "Ders Geçmişi", "Lessons", "Index", "🕘"),
            ("payments", "Ödemeler", "Payments", "Index", "💳"),
            ("reports", "Raporlar", "Reports", "Index", "📊")
        };

        return items.Select(item => new NavigationItemViewModel
        {
            Key = item.Key,
            Title = item.Title,
            Controller = item.Controller,
            Action = item.Action,
            IconText = item.IconText,
            IsActive = IsRouteMatch(item.Controller, item.Action, currentController, currentAction)
        }).ToList();
    }

    public IReadOnlyList<NavigationItemViewModel> GetTopbarItems(string? currentController, string? currentAction)
    {
        var items = new (string Key, string Title, string Controller, string Action, string IconText)[]
        {
            ("notifications", "Bildirimler", "Notifications", "Index", "🔔"),
            ("profile", "Profil", "Account", "Profile", "👤"),
            ("settings", "Ayarlar", "Settings", "Index", "⚙️")
        };

        return items.Select(item => new NavigationItemViewModel
        {
            Key = item.Key,
            Title = item.Title,
            Controller = item.Controller,
            Action = item.Action,
            IconText = item.IconText,
            IsActive = IsRouteMatch(item.Controller, item.Action, currentController, currentAction)
        }).ToList();
    }

    private static bool IsRouteMatch(
        string itemController,
        string itemAction,
        string? currentController,
        string? currentAction)
    {
        if (string.IsNullOrWhiteSpace(currentController))
        {
            return false;
        }

        if (!string.Equals(itemController, currentController, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (string.Equals(itemController, "Home", StringComparison.OrdinalIgnoreCase))
        {
            return string.IsNullOrWhiteSpace(currentAction) ||
                   string.Equals(itemAction, currentAction, StringComparison.OrdinalIgnoreCase);
        }

        return true;
    }
}
