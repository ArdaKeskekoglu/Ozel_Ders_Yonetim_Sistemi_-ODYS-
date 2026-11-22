using Microsoft.AspNetCore.Identity;

namespace OzelDers.Web.Domain.Entities;

/// <summary>
/// Özel Ders Yönetim Sistemi öğretmen kullanıcı hesabı modeli (FR-006, FR-009, DEC-10).
/// İlk sürümde yalnızca öğretmen hesapları bulunur; öğrenciler için ayrı kullanıcı hesabı yoktur.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = "Turkey Standard Time";

    public string CurrencyCode { get; set; } = "TRY";

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();
}
