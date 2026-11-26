using OzelDers.Web.Services;

namespace OzelDers.UnitTests;

public class NavigationServiceTests
{
    [Fact]
    public void GetSidebarItems_ReturnsAvailableAndUpcomingModulesInExpectedOrder()
    {
        var service = new NavigationService();

        var items = service.GetSidebarItems("Home", "Index");

        Assert.Equal(
            new[]
            {
                "Ana Sayfa",
                "Öğrenciler",
                "Ders Programı",
                "Ders Geçmişi",
                "Ödemeler",
                "Raporlar"
            },
            items.Select(i => i.Title).ToArray());

        Assert.DoesNotContain(items, i => i.Key == "subjects");
        Assert.All(items.Take(3), i => Assert.Null(i.BadgeText));
        Assert.All(items.Skip(3), i =>
        {
            Assert.Equal("Yakında", i.BadgeText);
            Assert.False(i.IsActive);
        });
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
    public void GetTopbarItems_ShowsProfileAndUpcomingModules()
    {
        var service = new NavigationService();

        var items = service.GetTopbarItems("Subjects", "Index");

        Assert.Equal(
            new[] { "Bildirimler", "Profil", "Ayarlar" },
            items.Select(i => i.Title).ToArray());

        var active = Assert.Single(items, i => i.IsActive);
        Assert.Equal("profile", active.Key);
        Assert.Null(active.BadgeText);
        Assert.All(items.Where(i => i.Key != "profile"), i => Assert.Equal("Yakında", i.BadgeText));
    }

    [Fact]
    public void GetSidebarItems_MarksStudentSubjectsAsStudents()
    {
        var service = new NavigationService();

        var items = service.GetSidebarItems("StudentSubjects", "Edit");

        Assert.Equal("students", Assert.Single(items, item => item.IsActive).Key);
    }
}
