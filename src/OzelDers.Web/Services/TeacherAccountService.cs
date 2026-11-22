using Microsoft.AspNetCore.Identity;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Account;

namespace OzelDers.Web.Services;

/// <summary>
/// Öğretmen kayıt, giriş doğrulama, profil düzenleme, şifre değiştirme ve
/// güvenli token ile şifre sıfırlama işlemlerini yürüten servis (FR-001..FR-006, NFR-005, NFR-009, NFR-010).
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

    public async Task<ProfileViewModel?> GetProfileAsync(
        string teacherId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return null;
        }

        var user = await _userManager.FindByIdAsync(teacherId);
        if (user is null)
        {
            return null;
        }

        return new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            TimeZoneId = user.TimeZoneId,
            CurrencyCode = user.CurrencyCode
        };
    }

    public async Task<TeacherAccountOperationResult> UpdateProfileAsync(
        string teacherId,
        ProfileViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return new TeacherAccountOperationResult(false, new[] { "Geçerli bir öğretmen oturumu bulunamadı." });
        }

        var user = await _userManager.FindByIdAsync(teacherId);
        if (user is null)
        {
            return new TeacherAccountOperationResult(false, new[] { "Öğretmen hesabı bulunamadı." });
        }

        var firstName = model.FirstName.Trim();
        var lastName = model.LastName.Trim();
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            return new TeacherAccountOperationResult(false, new[] { "Ad ve soyad alanları boş bırakılamaz." });
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.DisplayName = string.IsNullOrWhiteSpace(model.DisplayName)
            ? $"{firstName} {lastName}".Trim()
            : model.DisplayName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        user.TimeZoneId = string.IsNullOrWhiteSpace(model.TimeZoneId)
            ? "Turkey Standard Time"
            : model.TimeZoneId.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            var errors = updateResult.Errors
                .Select(MapIdentityErrorToTurkish)
                .Distinct()
                .ToList();
            return new TeacherAccountOperationResult(false, errors);
        }

        return new TeacherAccountOperationResult(true, Array.Empty<string>());
    }

    public async Task<TeacherAccountOperationResult> ChangePasswordAsync(
        string teacherId,
        ChangePasswordViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return new TeacherAccountOperationResult(false, new[] { "Geçerli bir öğretmen oturumu bulunamadı." });
        }

        if (!string.Equals(model.NewPassword, model.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return new TeacherAccountOperationResult(false, new[] { "Yeni şifre ile şifre tekrarı birbiriyle eşleşmiyor." });
        }

        var user = await _userManager.FindByIdAsync(teacherId);
        if (user is null)
        {
            return new TeacherAccountOperationResult(false, new[] { "Öğretmen hesabı bulunamadı." });
        }

        var changeResult = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!changeResult.Succeeded)
        {
            var errors = changeResult.Errors
                .Select(MapIdentityErrorToTurkish)
                .Distinct()
                .ToList();
            return new TeacherAccountOperationResult(false, errors);
        }

        return new TeacherAccountOperationResult(true, Array.Empty<string>());
    }

    public async Task<string?> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var user = await _userManager.FindByEmailAsync(email.Trim());
        if (user is null)
        {
            return null;
        }

        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<TeacherAccountOperationResult> ResetPasswordAsync(
        ResetPasswordViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (!string.Equals(model.NewPassword, model.ConfirmNewPassword, StringComparison.Ordinal))
        {
            return new TeacherAccountOperationResult(false, new[] { "Yeni şifre ile şifre tekrarı birbiriyle eşleşmiyor." });
        }

        if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Token))
        {
            return new TeacherAccountOperationResult(false, new[] { "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş." });
        }

        var user = await _userManager.FindByEmailAsync(model.Email.Trim());
        if (user is null)
        {
            // Kullanıcı numaralandırma (enumeration) koruması için aynı güvenli mesajı döndür
            return new TeacherAccountOperationResult(false, new[] { "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş." });
        }

        var resetResult = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!resetResult.Succeeded)
        {
            var errors = resetResult.Errors
                .Select(MapIdentityErrorToTurkish)
                .Distinct()
                .ToList();
            return new TeacherAccountOperationResult(false, errors);
        }

        return new TeacherAccountOperationResult(true, Array.Empty<string>());
    }

    private static string MapIdentityErrorToTurkish(IdentityError error) =>
        error.Code switch
        {
            "DuplicateEmail" or "DuplicateUserName" =>
                "Bu e-posta adresi ile kayıtlı bir öğretmen hesabı zaten mevcut.",
            "InvalidEmail" or "InvalidUserName" =>
                "Lütfen geçerli bir e-posta adresi giriniz.",
            "PasswordMismatch" =>
                "Girdiğiniz mevcut şifre hatalı.",
            "InvalidToken" =>
                "Şifre sıfırlama bağlantısı geçersiz veya süresi dolmuş.",
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
            _ => "İşlem sırasında bilgiler doğrulanamadı. Lütfen girdiğiniz bilgileri kontrol ediniz."
        };
}
