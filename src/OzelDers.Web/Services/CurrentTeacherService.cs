using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OzelDers.Web.Domain.Common;
using OzelDers.Web.Services.Abstractions;

namespace OzelDers.Web.Services;

/// <summary>
/// Oturumdaki öğretmen kimliğini ClaimsPrincipal üzerinden okur ve
/// öğretmene ait olmayan kayıtlara erişimi reddeder (FR-007, FR-008, NFR-007).
/// </summary>
public class CurrentTeacherService : ICurrentTeacherService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTeacherService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated == true &&
        !string.IsNullOrWhiteSpace(TeacherId);

    public string? TeacherId =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string GetRequiredTeacherId()
    {
        var teacherId = TeacherId;
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Bu işlem için öğretmen oturumu açılmış olmalıdır.");
        }

        return teacherId;
    }

    public bool IsOwner(ITeacherOwnedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var currentTeacherId = TeacherId;
        if (string.IsNullOrWhiteSpace(currentTeacherId) || string.IsNullOrWhiteSpace(entity.TeacherId))
        {
            return false;
        }

        return string.Equals(currentTeacherId, entity.TeacherId, StringComparison.Ordinal);
    }

    public void EnsureOwnership(ITeacherOwnedEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var currentTeacherId = GetRequiredTeacherId();
        if (!string.Equals(currentTeacherId, entity.TeacherId, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException("Başka bir öğretmene ait veriye erişim yetkiniz bulunmamaktadır.");
        }
    }

    public IQueryable<T> FilterByCurrentTeacher<T>(IQueryable<T> query) where T : class, ITeacherOwnedEntity
    {
        ArgumentNullException.ThrowIfNull(query);

        var currentTeacherId = GetRequiredTeacherId();
        return query.Where(entity => entity.TeacherId == currentTeacherId);
    }
}
