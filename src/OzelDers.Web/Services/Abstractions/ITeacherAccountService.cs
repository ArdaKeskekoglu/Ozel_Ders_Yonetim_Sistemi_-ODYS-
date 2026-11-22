using OzelDers.Web.Domain.Entities;
using OzelDers.Web.ViewModels.Account;

namespace OzelDers.Web.Services.Abstractions;

public sealed record TeacherRegistrationResult(
    bool Succeeded,
    ApplicationUser? User,
    IReadOnlyList<string> Errors);

public sealed record TeacherLoginValidationResult(
    bool Succeeded,
    ApplicationUser? User,
    bool IsLockedOut,
    string? ErrorMessage);

/// <summary>
/// Öğretmen hesap kayıt, giriş doğrulama ve yönetim işlemlerini yürüten servis sözleşmesi.
/// </summary>
public interface ITeacherAccountService
{
    Task<TeacherRegistrationResult> RegisterTeacherAsync(
        RegisterViewModel model,
        CancellationToken cancellationToken = default);

    Task<TeacherLoginValidationResult> ValidateTeacherCredentialsAsync(
        LoginViewModel model,
        CancellationToken cancellationToken = default);
}
