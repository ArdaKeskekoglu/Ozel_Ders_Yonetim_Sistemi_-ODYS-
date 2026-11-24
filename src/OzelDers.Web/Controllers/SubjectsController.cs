using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Subjects;

namespace OzelDers.Web.Controllers;

[Authorize]
public class SubjectsController : Controller
{
    private readonly ISubjectService _subjectService;
    private readonly ICurrentTeacherService _currentTeacherService;

    public SubjectsController(
        ISubjectService subjectService,
        ICurrentTeacherService currentTeacherService)
    {
        _subjectService = subjectService;
        _currentTeacherService = currentTeacherService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        SubjectStatusFilter status = SubjectStatusFilter.Active,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _subjectService.GetSubjectsForTeacherAsync(
            teacherId,
            status,
            cancellationToken);

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateSubjectViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateSubjectViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _subjectService.CreateSubjectAsync(teacherId, model, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = result.InfoMessage ?? "Ders türü başarıyla eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _subjectService.GetSubjectForEditAsync(teacherId, id, cancellationToken);
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
        EditSubjectViewModel model,
        CancellationToken cancellationToken = default)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _subjectService.UpdateSubjectAsync(teacherId, model, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = result.InfoMessage ?? "Ders türü başarıyla güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int id, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _subjectService.SetDefaultSubjectAsync(teacherId, id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Varsayılan ders değiştirilemedi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = result.InfoMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Archive(int id, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _subjectService.SetSubjectArchiveStatusAsync(teacherId, id, isArchived: true, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Ders türü arşivlenemedi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = result.InfoMessage;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unarchive(int id, CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _subjectService.SetSubjectArchiveStatusAsync(teacherId, id, isArchived: false, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["ErrorMessage"] = result.Errors.FirstOrDefault() ?? "Ders türü yeniden aktif edilemedi.";
            return RedirectToAction(nameof(Index));
        }

        TempData["SuccessMessage"] = result.InfoMessage;
        return RedirectToAction(nameof(Index));
    }
}
