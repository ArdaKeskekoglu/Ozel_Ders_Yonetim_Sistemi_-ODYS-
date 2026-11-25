using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Schedule;

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

    [HttpGet]
    public async Task<IActionResult> Create(
        DateOnly? date = null,
        int? studentId = null,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _scheduleService.PrepareCreateLessonViewModelAsync(
            teacherId,
            date,
            studentId,
            existingModel: null,
            cancellationToken);

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateLessonViewModel model,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();

        if (!ModelState.IsValid)
        {
            var populated = await _scheduleService.PrepareCreateLessonViewModelAsync(
                teacherId,
                model.LessonDate,
                model.PreselectedStudentId,
                model,
                cancellationToken);

            return View(populated);
        }

        var result = await _scheduleService.CreateLessonAsync(
            teacherId,
            model,
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var populated = await _scheduleService.PrepareCreateLessonViewModelAsync(
                teacherId,
                model.LessonDate,
                model.PreselectedStudentId,
                model,
                cancellationToken);

            return View(populated);
        }

        TempData["SuccessMessage"] = result.InfoMessage ?? "Ders oturumu takvime başarıyla eklendi.";
        return RedirectToAction(nameof(Index), new { date = model.LessonDate.ToString("yyyy-MM-dd") });
    }
}
