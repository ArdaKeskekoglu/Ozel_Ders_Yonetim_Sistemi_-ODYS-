using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Account;

namespace OzelDers.Web.Services;

/// <summary>
/// Öğretmen kayıt, giriş doğrulama, profil düzenleme, varsayılan ders bütünlüğü,
/// şifre değiştirme ve güvenli token ile şifre sıfırlama işlemlerini yürüten servis
/// (FR-001..FR-006, FR-022, BR-16, NFR-005, NFR-008, NFR-009, NFR-010).
/// </summary>
public class TeacherAccountService : ITeacherAccountService
{
    private const string InvalidCredentialsMessage = "E-posta adresi veya şifre hatalı.";
    private const string LockedOutMessage = "Çok fazla başarısız giriş denemesi nedeniyle hesabınız geçici olarak kilitlendi. Lütfen daha sonra tekrar deneyiniz.";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;

    public TeacherAccountService(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
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

        var subjectNames = model.GetNormalizedSubjectNames();
        if (subjectNames.Count == 0)
        {
            return new TeacherRegistrationResult(
                false,
                null,
                new[] { "Kayıt olabilmek için en az bir ders türü seçmeli veya tanımlamalısınız." });
        }

        if (subjectNames.Any(s => s.Length < 2 || s.Length > 100))
        {
            return new TeacherRegistrationResult(
                false,
                null,
                new[] { "Her bir ders türü adı en az 2, en fazla 100 karakter olmalıdır." });
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

        var isRelational = _dbContext.Database.IsRelational();
        await using var transaction = isRelational
            ? await _dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        try
        {
            var identityResult = await _userManager.CreateAsync(teacher, model.Password);
            if (!identityResult.Succeeded)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);
                }

                var friendlyErrors = identityResult.Errors
                    .Select(MapIdentityErrorToTurkish)
                    .Distinct()
                    .ToList();

                return new TeacherRegistrationResult(false, null, friendlyErrors);
            }

            var subjects = subjectNames
                .Select((name, index) => new Subject
                {
                    TeacherId = teacher.Id,
                    Name = name,
                    IsDefault = index == 0,
                    IsArchived = false
                })
                .ToList();

            _dbContext.Subjects.AddRange(subjects);
            await _dbContext.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

            return new TeacherRegistrationResult(true, teacher, Array.Empty<string>());
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else
            {
                // InMemory test ortamında transaction desteklenmediğinde kısmi kullanıcı kaydını temizle
                var created = await _userManager.FindByIdAsync(teacher.Id);
                if (created is not null)
                {
                    await _userManager.DeleteAsync(created);
                }
            }

            return new TeacherRegistrationResult(
                false,
                null,
                new[] { "Kayıt işlemi sırasında beklenmeyen bir hata oluştu. Lütfen tekrar deneyiniz." });
        }
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

        await EnsureDefaultSubjectInvariantAsync(teacherId, cancellationToken);

        var activeSubjects = await _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId && !s.IsArchived)
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var defaultSubject = activeSubjects.FirstOrDefault(s => s.IsDefault)
            ?? activeSubjects.FirstOrDefault();

        return new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            DisplayName = user.DisplayName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            TimeZoneId = user.TimeZoneId,
            CurrencyCode = user.CurrencyCode,
            DefaultSubjectName = defaultSubject?.Name,
            ActiveSubjectNames = activeSubjects.Select(s => s.Name).ToList()
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

        if (!string.IsNullOrWhiteSpace(model.InitialSubjectName))
        {
            var initialSubject = model.InitialSubjectName.Trim();
            if (initialSubject.Length < 2 || initialSubject.Length > 100)
            {
                return new TeacherAccountOperationResult(
                    false,
                    new[] { "Ders türü adı en az 2, en fazla 100 karakter olmalıdır." });
            }

            var hasAnyActiveSubject = await _dbContext.Subjects
                .AnyAsync(s => s.TeacherId == teacherId && !s.IsArchived, cancellationToken);

            if (!hasAnyActiveSubject)
            {
                var existingArchived = await _dbContext.Subjects
                    .FirstOrDefaultAsync(
                        s => s.TeacherId == teacherId && s.Name.ToLower() == initialSubject.ToLower(),
                        cancellationToken);

                if (existingArchived is not null)
                {
                    existingArchived.IsArchived = false;
                    existingArchived.IsDefault = true;
                }
                else
                {
                    _dbContext.Subjects.Add(new Subject
                    {
                        TeacherId = teacherId,
                        Name = initialSubject,
                        IsDefault = true,
                        IsArchived = false
                    });
                }

                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        await EnsureDefaultSubjectInvariantAsync(teacherId, cancellationToken);

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

    /// <summary>
    /// Öğretmenin en az bir aktif dersi varken mutlaka tam olarak bir aktif varsayılan dersi
    /// bulunmasını garanti altına alır (BR-16).
    /// </summary>
    private async Task EnsureDefaultSubjectInvariantAsync(
        string teacherId,
        CancellationToken cancellationToken)
    {
        var activeSubjects = await _dbContext.Subjects
            .Where(s => s.TeacherId == teacherId && !s.IsArchived)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        if (activeSubjects.Count == 0)
        {
            return;
        }

        var defaultSubjects = activeSubjects.Where(s => s.IsDefault).ToList();
        if (defaultSubjects.Count == 1)
        {
            return;
        }

        if (defaultSubjects.Count == 0)
        {
            activeSubjects[0].IsDefault = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        // Birden fazla varsayılan işaretlenmişse yalnızca ilkini koru
        for (var i = 1; i < defaultSubjects.Count; i++)
        {
            defaultSubjects[i].IsDefault = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
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
