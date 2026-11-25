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

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _scheduleService.GetLessonForEditAsync(
            teacherId,
            id,
            existingModel: null,
            cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        EditLessonViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;
        var teacherId = _currentTeacherService.GetRequiredTeacherId();

        if (!ModelState.IsValid)
        {
            var populated = await _scheduleService.GetLessonForEditAsync(
                teacherId,
                id,
                model,
                cancellationToken);

            if (populated is null)
            {
                return NotFound();
            }

            return View(populated);
        }

        var result = await _scheduleService.UpdateLessonAsync(
            teacherId,
            model,
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var populated = await _scheduleService.GetLessonForEditAsync(
                teacherId,
                id,
                model,
                cancellationToken);

            if (populated is null)
            {
                return NotFound();
            }

            return View(populated);
        }

        TempData["SuccessMessage"] = result.InfoMessage ?? "Ders oturumu başarıyla güncellendi.";
        return RedirectToAction(nameof(Index), new { date = model.LessonDate.ToString("yyyy-MM-dd") });
    }

    [HttpGet]
    public async Task<IActionResult> Reschedule(
        int id,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _scheduleService.GetLessonForRescheduleAsync(
            teacherId,
            id,
            cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reschedule(
        int id,
        RescheduleLessonViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.LessonId = id;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _scheduleService.RescheduleLessonAsync(
            teacherId,
            model,
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = result.InfoMessage ?? "Ders oturumu ertelendi.";
        return RedirectToAction(nameof(Index), new { date = model.NewDate.ToString("yyyy-MM-dd") });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(
        int id,
        string? cancelReason = null,
        DateOnly? returnDate = null,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _scheduleService.CancelLessonAsync(
            teacherId,
            id,
            cancelReason,
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Ders oturumu iptal edilemedi.";
        }
        else
        {
            TempData["SuccessMessage"] = result.InfoMessage;
        }

        return RedirectToAction(
            nameof(Index),
            returnDate.HasValue ? new { date = returnDate.Value.ToString("yyyy-MM-dd") } : null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Restore(
        int id,
        DateOnly? returnDate = null,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _scheduleService.RestoreCancelledLessonAsync(
            teacherId,
            id,
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "İptal edilen ders yeniden aktifleştirilemedi.";
        }
        else
        {
            TempData["SuccessMessage"] = result.InfoMessage;
        }

        return RedirectToAction(
            nameof(Index),
            returnDate.HasValue ? new { date = returnDate.Value.ToString("yyyy-MM-dd") } : null);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(
        int id,
        DateOnly? returnDate = null,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _scheduleService.DeleteLessonAsync(
            teacherId,
            id,
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Ders kaydı kaldırılamadı.";
        }
        else
        {
            TempData["SuccessMessage"] = result.InfoMessage;
        }

        return RedirectToAction(
            nameof(Index),
            returnDate.HasValue ? new { date = returnDate.Value.ToString("yyyy-MM-dd") } : null);
    }
}
