using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Web.Services.Abstractions;

namespace OzelDers.Web.Controllers;

[Authorize]
public class ScheduleController : Controller
{
    private readonly IScheduleService _scheduleService;
    private readonly ICurrentTeacherService _currentTeacherService;

    public ScheduleController(
        IScheduleService scheduleService,
        ICurrentTeacherService currentTeacherService)
    {
        _scheduleService = scheduleService;
        _currentTeacherService = currentTeacherService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _scheduleService.GetWeeklyScheduleAsync(
            teacherId,
            date,
            cancellationToken);

        return View(viewModel);
    }
}
