using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Navigation;

namespace OzelDers.Web.Services;

/// <summary>
/// Kullanılabilir sayfaları bağlantı, gelecekteki modülleri pasif öğe olarak gösterir.
/// Ders türü yönetimine Profil üzerinden erişilir.
/// </summary>
public class NavigationService : INavigationService
{
    public IReadOnlyList<NavigationItemViewModel> GetSidebarItems(string? currentController, string? currentAction)
    {
        var items = new (string Key, string Title, string Controller, string Action, string IconText, string? BadgeText)[]
        {
            ("dashboard", "Ana Sayfa", "Home", "Index", "⌂", null),
            ("students", "Öğrenciler", "Students", "Index", "👥", null),
            ("schedule", "Ders Programı", "Schedule", "Index", "📅", null),
            ("lessons", "Ders Geçmişi", "Lessons", "Index", "◷", "Yakında"),
            ("payments", "Ödemeler", "Payments", "Index", "▣", "Yakında"),
            ("reports", "Raporlar", "Reports", "Index", "▥", "Yakında")
        };

        return items.Select(item => new NavigationItemViewModel
        {
            Key = item.Key,
            Title = item.Title,
            Controller = item.Controller,
            Action = item.Action,
            IconText = item.IconText,
            BadgeText = item.BadgeText,
            IsActive = item.BadgeText is null && IsRouteMatch(item.Controller, item.Action, currentController, currentAction)
        }).ToList();
    }

    public IReadOnlyList<NavigationItemViewModel> GetTopbarItems(string? currentController, string? currentAction)
    {
        var items = new (string Key, string Title, string Controller, string Action, string IconText, string? BadgeText)[]
        {
            ("notifications", "Bildirimler", "Notifications", "Index", "♧", "Yakında"),
            ("profile", "Profil", "Account", "Profile", "👤", null),
            ("settings", "Ayarlar", "Settings", "Index", "⚙", "Yakında")
        };

        return items.Select(item => new NavigationItemViewModel
        {
            Key = item.Key,
            Title = item.Title,
            Controller = item.Controller,
            Action = item.Action,
            IconText = item.IconText,
            BadgeText = item.BadgeText,
            IsActive = item.BadgeText is null && IsRouteMatch(item.Controller, item.Action, currentController, currentAction)
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

        if (string.Equals(itemController, "Students", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(currentController, "StudentSubjects", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(itemController, "Account", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(currentController, "Subjects", StringComparison.OrdinalIgnoreCase))
        {
            return true;
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
