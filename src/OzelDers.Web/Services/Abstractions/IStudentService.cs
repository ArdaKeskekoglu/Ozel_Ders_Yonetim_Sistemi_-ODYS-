using OzelDers.Web.ViewModels.Students;

namespace OzelDers.Web.Services.Abstractions;

/// <summary>
/// Öğrenci listeleme, arama ve yönetim işlemlerini yürüten servis sözleşmesi (FR-007, FR-013..FR-015).
/// </summary>
public interface IStudentService
{
    Task<StudentListFilterViewModel> GetStudentsForTeacherAsync(
        string teacherId,
        string? searchTerm = null,
        StudentStatusFilter statusFilter = StudentStatusFilter.Active,
        CancellationToken cancellationToken = default);
}
