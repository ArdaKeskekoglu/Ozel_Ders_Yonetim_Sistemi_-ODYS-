using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Students;

namespace OzelDers.Web.Services;

/// <summary>
/// Yalnızca oturumdaki öğretmenin kendi öğrencilerini listeleyen, arayan, ekleyen,
/// güncelleyen ve arşiv durumunu yöneten servis (FR-007, FR-010..FR-021, DEC-13, NFR-007..NFR-009).
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

    public async Task<StudentOperationResult> CreateStudentAsync(
        string teacherId,
        CreateStudentViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Yeni öğrenci eklemek için öğretmen oturumu gereklidir.");
        }

        var firstName = model.FirstName?.Trim() ?? string.Empty;
        var lastName = model.LastName?.Trim() ?? string.Empty;

        var errors = ValidateNames(firstName, lastName);
        if (errors.Count > 0)
        {
            return new StudentOperationResult(false, null, errors);
        }

        var student = new Student
        {
            TeacherId = teacherId,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim(),
            Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim(),
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
            IsArchived = false
        };

        _dbContext.Students.Add(student);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StudentOperationResult(true, student, Array.Empty<string>());
    }

    public async Task<EditStudentViewModel?> GetStudentForEditAsync(
        string teacherId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Öğrenci bilgilerine erişmek için öğretmen oturumu gereklidir.");
        }

        return await _dbContext.Students
            .AsNoTracking()
            .Where(s => s.Id == studentId && s.TeacherId == teacherId)
            .Select(s => new EditStudentViewModel
            {
                Id = s.Id,
                FirstName = s.FirstName,
                LastName = s.LastName,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email,
                Notes = s.Notes,
                IsArchived = s.IsArchived
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<StudentOperationResult> UpdateStudentAsync(
        string teacherId,
        EditStudentViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Öğrenci bilgilerini güncellemek için öğretmen oturumu gereklidir.");
        }

        var student = await _dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == model.Id && s.TeacherId == teacherId, cancellationToken);

        if (student is null)
        {
            return new StudentOperationResult(
                false,
                null,
                new[] { "Öğrenci kaydı bulunamadı veya bu kaydı düzenleme yetkiniz yok." });
        }

        var firstName = model.FirstName?.Trim() ?? string.Empty;
        var lastName = model.LastName?.Trim() ?? string.Empty;

        var errors = ValidateNames(firstName, lastName);
        if (errors.Count > 0)
        {
            return new StudentOperationResult(false, null, errors);
        }

        student.FirstName = firstName;
        student.LastName = lastName;
        student.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();
        student.Email = string.IsNullOrWhiteSpace(model.Email) ? null : model.Email.Trim();
        student.Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StudentOperationResult(true, student, Array.Empty<string>());
    }

    public async Task<StudentOperationResult> SetStudentArchiveStatusAsync(
        string teacherId,
        int studentId,
        bool isArchived,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Öğrenci arşiv durumunu değiştirmek için öğretmen oturumu gereklidir.");
        }

        var student = await _dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == studentId && s.TeacherId == teacherId, cancellationToken);

        if (student is null)
        {
            return new StudentOperationResult(
                false,
                null,
                new[] { "Öğrenci kaydı bulunamadı veya bu işlem için yetkiniz yok." });
        }

        if (student.IsArchived != isArchived)
        {
            student.IsArchived = isArchived;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new StudentOperationResult(true, student, Array.Empty<string>());
    }

    private static List<string> ValidateNames(string firstName, string lastName)
    {
        var errors = new List<string>();
        if (firstName.Length < 2)
        {
            errors.Add("Öğrenci adı en az 2 karakter olmalıdır.");
        }

        if (lastName.Length < 2)
        {
            errors.Add("Öğrenci soyadı en az 2 karakter olmalıdır.");
        }

        return errors;
    }
}
