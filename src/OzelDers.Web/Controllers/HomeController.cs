using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Web.Models;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Home;

namespace OzelDers.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ISystemInfoService _systemInfoService;

    public HomeController(
        ILogger<HomeController> logger,
        ISystemInfoService systemInfoService)
    {
        _logger = logger;
        _systemInfoService = systemInfoService;
    }

    public IActionResult Index()
    {
        var viewModel = new HomeIndexViewModel
        {
            ApplicationName = _systemInfoService.GetApplicationName(),
            ShortCode = _systemInfoService.GetShortCode(),
            Description = _systemInfoService.GetDescription()
        };

        return View(viewModel);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
