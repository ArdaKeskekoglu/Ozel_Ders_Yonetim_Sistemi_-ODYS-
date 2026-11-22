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

public sealed record TeacherAccountOperationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors);

/// <summary>
/// Öğretmen hesap kayıt, giriş doğrulama, profil ve şifre yönetim işlemlerini yürüten servis sözleşmesi.
/// </summary>
public interface ITeacherAccountService
{
    Task<TeacherRegistrationResult> RegisterTeacherAsync(
        RegisterViewModel model,
        CancellationToken cancellationToken = default);

    Task<TeacherLoginValidationResult> ValidateTeacherCredentialsAsync(
        LoginViewModel model,
        CancellationToken cancellationToken = default);

    Task<ProfileViewModel?> GetProfileAsync(
        string teacherId,
        CancellationToken cancellationToken = default);

    Task<TeacherAccountOperationResult> UpdateProfileAsync(
        string teacherId,
        ProfileViewModel model,
        CancellationToken cancellationToken = default);

    Task<TeacherAccountOperationResult> ChangePasswordAsync(
        string teacherId,
        ChangePasswordViewModel model,
        CancellationToken cancellationToken = default);

    Task<string?> GeneratePasswordResetTokenAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<TeacherAccountOperationResult> ResetPasswordAsync(
        ResetPasswordViewModel model,
        CancellationToken cancellationToken = default);
}
