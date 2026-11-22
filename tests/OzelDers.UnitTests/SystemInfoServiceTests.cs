using OzelDers.Web.Services;

namespace OzelDers.UnitTests;

public class SystemInfoServiceTests
{
    [Fact]
    public void GetApplicationName_ReturnsExpectedName()
    {
        var service = new SystemInfoService();

        Assert.Equal("Özel Ders Yönetim Sistemi", service.GetApplicationName());
    }

    [Fact]
    public void GetShortCode_ReturnsOdys()
    {
        var service = new SystemInfoService();

        Assert.Equal("ÖDYS", service.GetShortCode());
    }

    [Fact]
    public void GetDescription_ReturnsNonEmptyDescription()
    {
        var service = new SystemInfoService();

        var description = service.GetDescription();

        Assert.False(string.IsNullOrWhiteSpace(description));
        Assert.Contains("otomatik ders programlama", description);
    }
}
