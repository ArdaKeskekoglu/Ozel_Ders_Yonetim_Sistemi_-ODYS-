using OzelDers.Web.Domain.Common;

namespace OzelDers.Web.Services.Abstractions;

/// <summary>
/// Oturum açmış öğretmenin kimliğini sağlayan ve diğer öğretmenlerin verilerine
/// erişimi engelleyen sahiplik doğrulama servisi (FR-007, FR-008, NFR-007).
/// </summary>
public interface ICurrentTeacherService
{
    bool IsAuthenticated { get; }
    string? TeacherId { get; }
    string GetRequiredTeacherId();
    bool IsOwner(ITeacherOwnedEntity entity);
    void EnsureOwnership(ITeacherOwnedEntity entity);
    IQueryable<T> FilterByCurrentTeacher<T>(IQueryable<T> query) where T : class, ITeacherOwnedEntity;
}
