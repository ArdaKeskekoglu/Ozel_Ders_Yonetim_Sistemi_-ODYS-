using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Students;

namespace OzelDers.Web.Controllers;

[Authorize]
public class StudentsController : Controller
{
    private readonly IStudentService _studentService;
    private readonly ICurrentTeacherService _currentTeacherService;

    public StudentsController(
        IStudentService studentService,
        ICurrentTeacherService currentTeacherService)
    {
        _studentService = studentService;
        _currentTeacherService = currentTeacherService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm = null,
        StudentStatusFilter status = StudentStatusFilter.Active,
        CancellationToken cancellationToken = default)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var viewModel = await _studentService.GetStudentsForTeacherAsync(
            teacherId,
            searchTerm,
            status,
            cancellationToken);

        return View(viewModel);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateStudentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateStudentViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var result = await _studentService.CreateStudentAsync(teacherId, model, cancellationToken);
        if (!result.Succeeded || result.Student is null)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = $"{result.Student.FullName} adlı öğrenci başarıyla eklendi.";
        return RedirectToAction(nameof(Index));
    }
}
