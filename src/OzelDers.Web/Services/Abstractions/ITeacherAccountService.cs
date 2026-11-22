using OzelDers.Web.Domain.Entities;
using OzelDers.Web.ViewModels.Account;

namespace OzelDers.Web.Services.Abstractions;

public sealed record TeacherRegistrationResult(
    bool Succeeded,
    ApplicationUser? User,
    IReadOnlyList<string> Errors);

/// <summary>
/// Öğretmen hesap kayıt ve yönetim işlemlerini yürüten servis sözleşmesi.
/// </summary>
public interface ITeacherAccountService
{
    Task<TeacherRegistrationResult> RegisterTeacherAsync(
        RegisterViewModel model,
        CancellationToken cancellationToken = default);
}
