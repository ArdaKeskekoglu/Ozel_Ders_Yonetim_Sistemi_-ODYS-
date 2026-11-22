using Microsoft.AspNetCore.Identity;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Account;

namespace OzelDers.Web.Services;

/// <summary>
/// Öğretmen kayıt ve giriş doğrulama işlemlerini yürüten, Identity hata kodlarını
/// güvenli, anlaşılır Türkçe mesajlara dönüştüren servis (FR-001, FR-002, NFR-009, NFR-010).
/// </summary>
public class TeacherAccountService : ITeacherAccountService
{
    private const string InvalidCredentialsMessage = "E-posta adresi veya şifre hatalı.";
    private const string LockedOutMessage = "Çok fazla başarısız giriş denemesi nedeniyle hesabınız geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyiniz.";

    private readonly UserManager<ApplicationUser> _userManager;

    public TeacherAccountService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<TeacherRegistrationResult> RegisterTeacherAsync(
        RegisterViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        var normalizedEmail = model.Email.Trim();
        var firstName = model.FirstName.Trim();
        var lastName = model.LastName.Trim();
        var phone = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return new TeacherRegistrationResult(
                false,
                null,
                new[] { "Ad ve soyad alanları boş bırakılamaz." });
        }

        if (!string.Equals(model.Password, model.ConfirmPassword, StringComparison.Ordinal))
        {
            return new TeacherRegistrationResult(
                false,
                null,
                new[] { "Şifre ile şifre tekrarı birbiriyle eşleşmiyor." });
        }

        var existingUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingUser is not null)
        {
            return new TeacherRegistrationResult(
                false,
                null,
                new[] { "Bu e-posta adresi ile kayıtlı bir öğretmen hesabı zaten mevcut." });
        }

        var teacher = new ApplicationUser
        {
            UserName = normalizedEmail,
            Email = normalizedEmail,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{firstName} {lastName}".Trim(),
            PhoneNumber = phone,
            TimeZoneId = "Turkey Standard Time",
            CurrencyCode = "TRY",
            CreatedAtUtc = DateTime.UtcNow
        };

        var identityResult = await _userManager.CreateAsync(teacher, model.Password);
        if (!identityResult.Succeeded)
        {
            var friendlyErrors = identityResult.Errors
                .Select(MapIdentityErrorToTurkish)
                .Distinct()
                .ToList();

            return new TeacherRegistrationResult(false, null, friendlyErrors);
        }

        return new TeacherRegistrationResult(true, teacher, Array.Empty<string>());
    }

    public async Task<TeacherLoginValidationResult> ValidateTeacherCredentialsAsync(
        LoginViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
        {
            return new TeacherLoginValidationResult(false, null, false, InvalidCredentialsMessage);
        }

        var normalizedEmail = model.Email.Trim();
        var user = await _userManager.FindByEmailAsync(normalizedEmail);
        if (user is null)
        {
            return new TeacherLoginValidationResult(false, null, false, InvalidCredentialsMessage);
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            return new TeacherLoginValidationResult(false, null, true, LockedOutMessage);
        }

        var passwordValid = await _userManager.CheckPasswordAsync(user, model.Password);
        if (!passwordValid)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                return new TeacherLoginValidationResult(false, null, true, LockedOutMessage);
            }

            return new TeacherLoginValidationResult(false, null, false, InvalidCredentialsMessage);
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        return new TeacherLoginValidationResult(true, user, false, null);
    }

    private static string MapIdentityErrorToTurkish(IdentityError error) =>
        error.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName" =>
                "Bu e-posta adresi ile kayıtlı bir öğretmen hesabı zaten mevcut.",
            "InvalidEmail" or "InvalidUserName" =>
                "Lütfen geçerli bir e-posta adresi giriniz.",
            "PasswordTooShort" =>
                "Şifre en az 8 karakter uzunluğunda olmalıdır.",
            "PasswordRequiresDigit" =>
                "Şifre en az bir rakam (0-9) içermelidir.",
            "PasswordRequiresLower" =>
                "Şifre en az bir küçük harf (a-z) içermelidir.",
            "PasswordRequiresUpper" =>
                "Şifre en az bir büyük harf (A-Z) içermelidir.",
            "PasswordRequiresNonAlphanumeric" =>
                "Şifre en az bir özel karakter içermelidir.",
            _ => "Kayıt işlemi sırasında bilgiler doğrulanamadı. Lütfen girdiğiniz bilgileri kontrol ediniz."
        };
}
