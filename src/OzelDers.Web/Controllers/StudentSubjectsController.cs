using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.StudentSubjects;

namespace OzelDers.Web.Controllers;

[Authorize]
public class StudentSubjectsController : Controller
{
    private readonly IStudentSubjectService _studentSubjectService;
    private readonly ICurrentTeacherService _currentTeacherService;

    public StudentSubjectsController(
        IStudentSubjectService studentSubjectService,
        ICurrentTeacherService currentTeacherService)
    {
        _studentSubjectService = studentSubjectService;
        _currentTeacherService = currentTeacherService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _studentSubjectService.GetStudentSubjectsAsync(
            teacherId,
            studentId,
            cancellationToken);

        if (viewModel is null)
        {
            return NotFound();
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Assign(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _studentSubjectService.PrepareAssignViewModelAsync(
            teacherId,
            studentId,
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
    public async Task<IActionResult> Assign(
        AssignSubjectViewModel model,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();

        if (!ModelState.IsValid)
        {
            var populated = await _studentSubjectService.PrepareAssignViewModelAsync(
                teacherId,
                model.StudentId,
                model,
                cancellationToken);

            if (populated is null)
            {
                return NotFound();
            }

            return View(populated);
        }

        var result = await _studentSubjectService.AssignSubjectToStudentAsync(
            teacherId,
            model,
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var populated = await _studentSubjectService.PrepareAssignViewModelAsync(
                teacherId,
                model.StudentId,
                model,
                cancellationToken);

            if (populated is null)
            {
                return NotFound();
            }

            return View(populated);
        }

        TempData["SuccessMessage"] = result.InfoMessage ?? "Ders türü öğrenciye başarıyla atandı.";
        return RedirectToAction(nameof(Index), new { studentId = model.StudentId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _studentSubjectService.GetForEditAsync(
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
    public async Task<IActionResult> Edit(
        int id,
        EditStudentSubjectViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _studentSubjectService.UpdateStudentSubjectAsync(
            teacherId,
            model,
            cancellationToken);

        if (!result.Succeeded || result.StudentSubject is null)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = result.InfoMessage ?? "Öğrenci ders bilgileri güncellendi.";
        return RedirectToAction(nameof(Index), new { studentId = result.StudentSubject.StudentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(
        int id,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _studentSubjectService.SetActiveStatusAsync(
            teacherId,
            id,
            isActive: false,
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Ders ataması pasife alınamadı.";
            return RedirectToAction(nameof(Index), new { studentId });
        }

        TempData["SuccessMessage"] = result.InfoMessage;
        return RedirectToAction(nameof(Index), new { studentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Activate(
        int id,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _studentSubjectService.SetActiveStatusAsync(
            teacherId,
            id,
            isActive: true,
            cancellationToken);

        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Ders ataması yeniden aktifleştirilemedi.";
            return RedirectToAction(nameof(Index), new { studentId });
        }

        TempData["SuccessMessage"] = result.InfoMessage;
        return RedirectToAction(nameof(Index), new { studentId });
    }
}
