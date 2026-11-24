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
    private readonly ICurrentTeacherService _currentTeacherService;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ITeacherAccountService teacherAccountService,
        ICurrentTeacherService currentTeacherService,
        SignInManager<ApplicationUser> signInManager,
        IWebHostEnvironment environment,
        ILogger<AccountController> logger)
    {
        _teacherAccountService = teacherAccountService;
        _currentTeacherService = currentTeacherService;
        _signInManager = signInManager;
        _environment = environment;
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
    [Authorize]
    public async Task<IActionResult> Profile(CancellationToken cancellationToken)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        var profile = await _teacherAccountService.GetProfileAsync(teacherId, cancellationToken);
        if (profile is null)
        {
            return NotFound();
        }

        return View(profile);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model, CancellationToken cancellationToken)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        if (!ModelState.IsValid)
        {
            var currentProfile = await _teacherAccountService.GetProfileAsync(teacherId, cancellationToken);
            if (currentProfile is not null)
            {
                model.DefaultSubjectName = currentProfile.DefaultSubjectName;
                model.ActiveSubjectNames = currentProfile.ActiveSubjectNames;
            }

            return View(model);
        }

        var result = await _teacherAccountService.UpdateProfileAsync(teacherId, model, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            var currentProfile = await _teacherAccountService.GetProfileAsync(teacherId, cancellationToken);
            if (currentProfile is not null)
            {
                model.DefaultSubjectName = currentProfile.DefaultSubjectName;
                model.ActiveSubjectNames = currentProfile.ActiveSubjectNames;
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken cancellationToken)
    {
        var teacherId = _currentTeacherService.GetRequiredTeacherId();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _teacherAccountService.ChangePasswordAsync(teacherId, model, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Şifreniz başarıyla güncellendi.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new ForgotPasswordViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var token = await _teacherAccountService.GeneratePasswordResetTokenAsync(model.Email, cancellationToken);
        ViewData["RequestSubmitted"] = true;

        if (_environment.IsDevelopment() && !string.IsNullOrWhiteSpace(token))
        {
            ViewData["DevResetLink"] = Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email = model.Email.Trim(), token });
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email = null, string? token = null)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            TempData["SuccessMessage"] = "Şifre sıfırlama bağlantısı eksik veya geçersiz.";
            return RedirectToAction(nameof(ForgotPassword));
        }

        return View(new ResetPasswordViewModel
        {
            Email = email,
            Token = token
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _teacherAccountService.ResetPasswordAsync(model, cancellationToken);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error);
            }

            return View(model);
        }

        TempData["SuccessMessage"] = "Şifreniz başarıyla sıfırlandı. Yeni şifrenizle giriş yapabilirsiniz.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View();
    }
}
