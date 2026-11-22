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

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var validation = await _teacherAccountService.ValidateTeacherCredentialsAsync(model, cancellationToken);
        if (!validation.Succeeded || validation.User is null)
        {
            ModelState.AddModelError(string.Empty, validation.ErrorMessage ?? "E-posta adresi veya şifre hatalı.");
            return View(model);
        }

        await _signInManager.SignInAsync(validation.User, isPersistent: model.RememberMe);
        _logger.LogInformation("Öğretmen oturum açtı: {UserId}", validation.User.Id);

        TempData["SuccessMessage"] = $"Tekrar hoş geldiniz, {validation.User.DisplayName}!";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        TempData["SuccessMessage"] = "Oturumunuz güvenli bir şekilde kapatıldı.";
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
