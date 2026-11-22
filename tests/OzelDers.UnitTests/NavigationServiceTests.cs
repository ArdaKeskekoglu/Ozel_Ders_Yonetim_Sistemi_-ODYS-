using OzelDers.Web.Services;

namespace OzelDers.UnitTests;

public class NavigationServiceTests
{
    [Fact]
    public void GetSidebarItems_ReturnsAllRequiredModulesInExpectedOrder()
    {
        var service = new NavigationService();

        var items = service.GetSidebarItems("Home", "Index");

        Assert.Equal(
            new[]
            {
                "Ana Sayfa",
                "Öğrenciler",
                "Ders Türleri",
                "Ders Programı",
                "Ders Geçmişi",
                "Ödemeler",
                "Raporlar"
            },
            items.Select(i => i.Title).ToArray());
    }

    [Fact]
    public void GetSidebarItems_MarksMatchingControllerAsActive()
    {
        var service = new NavigationService();

        var items = service.GetSidebarItems("Students", "Details");

        var activeItems = items.Where(i => i.IsActive).ToList();
        var singleActive = Assert.Single(activeItems);
        Assert.Equal("students", singleActive.Key);
    }

    [Fact]
    public void GetTopbarItems_ReturnsNotificationsProfileAndSettings()
    {
        var service = new NavigationService();

        var items = service.GetTopbarItems("Settings", "Index");

        Assert.Equal(
            new[] { "Bildirimler", "Profil", "Ayarlar" },
            items.Select(i => i.Title).ToArray());

        var active = Assert.Single(items, i => i.IsActive);
        Assert.Equal("settings", active.Key);
    }
}
