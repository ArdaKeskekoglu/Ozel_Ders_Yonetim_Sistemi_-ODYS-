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
}
