using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Account;

namespace OzelDers.Web.Controllers;

public class AccountController : Controller
{
    private readonly ITeacherAccountService _teacherAccountService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ITeacherAccountService teacherAccountService,
        SignInManager<ApplicationUser> signInManager,
        ILogger<AccountController> logger)
    {
        _teacherAccountService = teacherAccountService;
        _signInManager = signInManager;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Register()
    {
        return View(new RegisterViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _teacherAccountService.RegisterTeacherAsync(model, cancellationToken);
        if (!result.Succeeded || result.User is null)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        await _signInManager.SignInAsync(result.User, isPersistent: false);
        _logger.LogInformation("Yeni öğretmen hesabı oluşturuldu: {UserId}", result.User.Id);

        TempData["SuccessMessage"] = $"Hoş geldiniz, {result.User.DisplayName}! Öğretmen hesabınız başarıyla oluşturuldu.";
        return RedirectToAction("Index", "Home");
    }
}
