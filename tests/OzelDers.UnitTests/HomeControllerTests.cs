using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OzelDers.Web.Controllers;
using OzelDers.Web.Models;

namespace OzelDers.UnitTests;

public class HomeControllerTests
{
    [Fact]
    public void Index_ReturnsViewResult()
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance);

        var result = controller.Index();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Privacy_ReturnsViewResult()
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance);

        var result = controller.Privacy();

        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void Error_ReturnsViewResultWithErrorViewModel()
    {
        var controller = new HomeController(NullLogger<HomeController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "test-trace-id" }
            }
        };

        var result = controller.Error();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ErrorViewModel>(viewResult.Model);
        Assert.Equal("test-trace-id", model.RequestId);
        Assert.True(model.ShowRequestId);
    }
}
