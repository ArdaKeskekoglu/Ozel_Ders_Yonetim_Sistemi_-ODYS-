using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OzelDers.Web.Controllers;
using OzelDers.Web.Models;
using OzelDers.Web.Services;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Home;

namespace OzelDers.UnitTests;

public class HomeControllerTests
{
    private static HomeController CreateController()
    {
        var services = new ServiceCollection();
        services.AddScoped<ISystemInfoService, SystemInfoService>();
        using var provider = services.BuildServiceProvider();
        var systemInfoService = provider.GetRequiredService<ISystemInfoService>();

        return new HomeController(NullLogger<HomeController>.Instance, systemInfoService);
    }

    [Fact]
    public void Index_ReturnsViewResultWithHomeIndexViewModel()
    {
        var controller = CreateController();

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<HomeIndexViewModel>(viewResult.Model);
        Assert.Equal("Özel Ders Yönetim Sistemi", model.ApplicationName);
        Assert.Equal("ÖDYS", model.ShortCode);
        Assert.Equal("Özel Ders Yönetim Sistemi (ÖDYS)", model.FullTitle);
        Assert.False(string.IsNullOrWhiteSpace(model.Description));
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var controller = CreateController();

        var result = controller.Privacy();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_ReturnsViewResultWithErrorViewModel()
    {
        var controller = CreateController();
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { TraceIdentifier = "test-trace-id" }
        };

        var result = controller.Error();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
        Assert.Equal("test-trace-id", model.RequestId);
        Assert.True(model.ShowRequestId);
    }
}
