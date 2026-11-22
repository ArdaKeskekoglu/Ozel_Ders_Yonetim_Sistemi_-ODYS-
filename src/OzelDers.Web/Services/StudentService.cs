using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Students;

namespace OzelDers.Web.Services;

/// <summary>
/// Yalnızca oturumdaki öğretmenin kendi öğrencilerini listeleyen, isimle arayan ve
/// aktif/arşiv durumuna göre filtreleyen servis (FR-007, FR-013..FR-015, NFR-007).
/// </summary>
public class StudentService : IStudentService
{
    private readonly ApplicationDbContext _dbContext;

    public StudentService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentListFilterViewModel> GetStudentsForTeacherAsync(
        string teacherId,
        string? searchTerm = null,
        StudentStatusFilter statusFilter = StudentStatusFilter.Active,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Öğrenci listesine erişmek için öğretmen oturumu gereklidir.");
        }

        var normalizedSearch = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();

        var teacherStudentsQuery = _dbContext.Students
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId);

        var activeCount = await teacherStudentsQuery.CountAsync(s => !s.IsArchived, cancellationToken);
        var archivedCount = await teacherStudentsQuery.CountAsync(s => s.IsArchived, cancellationToken);

        var filteredQuery = statusFilter switch
        {
            StudentStatusFilter.Active => teacherStudentsQuery.Where(s => !s.IsArchived),
            StudentStatusFilter.Archived => teacherStudentsQuery.Where(s => s.IsArchived),
            _ => teacherStudentsQuery
        };

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var termLower = normalizedSearch.ToLower();
            filteredQuery = filteredQuery.Where(s =>
                s.FirstName.ToLower().Contains(termLower) ||
                s.LastName.ToLower().Contains(termLower) ||
                (s.FirstName + " " + s.LastName).ToLower().Contains(termLower));
        }

        var students = await filteredQuery
            .OrderBy(s => s.IsArchived)
            .ThenBy(s => s.FirstName)
            .ThenBy(s => s.LastName)
            .Select(s => new StudentListItemViewModel
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email,
                IsArchived = s.IsArchived,
                Notes = s.Notes,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new StudentListFilterViewModel
        {
            SearchTerm = normalizedSearch,
            Status = statusFilter,
            ActiveCount = activeCount,
            ArchivedCount = archivedCount,
            Students = students
        };
    }
}
