using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Students;
using OzelDers.Web.ViewModels.StudentSubjects;

namespace OzelDers.Web.Services;

/// <summary>
/// Yalnızca oturumdaki öğretmenin kendi öğrencilerini listeleyen, arayan, detaylarını getiren,
/// ekleyen, güncelleyen, özel notlarını ve arşiv durumunu yöneten servis (FR-007, FR-010..FR-021, DEC-13, NFR-007..NFR-009).
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
        int? subjectId = null,
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

        var availableSubjects = await _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId && !s.IsArchived)
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .Select(s => new StudentSubjectFilterOptionViewModel
            {
                Id = s.Id,
                Name = s.Name
            })
            .ToListAsync(cancellationToken);

        var filteredQuery = statusFilter switch
        {
            StudentStatusFilter.Active => teacherStudentsQuery.Where(s => !s.IsArchived),
            StudentStatusFilter.Archived => teacherStudentsQuery.Where(s => s.IsArchived),
            _ => teacherStudentsQuery
        };

        if (subjectId.HasValue && subjectId.Value > 0)
        {
            var targetSubjectId = subjectId.Value;
            filteredQuery = filteredQuery.Where(s =>
                s.StudentSubjects.Any(ss => ss.SubjectId == targetSubjectId && ss.IsActive));
        }

        if (!string.IsNullOrWhiteSpace(normalizedSearch))
        {
            var termLower = normalizedSearch.ToLower();
            filteredQuery = filteredQuery.Where(s =>
                s.FirstName.ToLower().Contains(termLower) ||
                s.LastName.ToLower().Contains(termLower) ||
                (s.FirstName + " " + s.LastName).ToLower().Contains(termLower) ||
                (s.PhoneNumber != null && s.PhoneNumber.ToLower().Contains(termLower)) ||
                (s.Email != null && s.Email.ToLower().Contains(termLower)) ||
                (s.Notes != null && s.Notes.ToLower().Contains(termLower)));
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
                CreatedAtUtc = s.CreatedAtUtc,
                ActiveSubjects = s.StudentSubjects
                    .Where(ss => ss.IsActive && ss.Subject != null)
                    .OrderByDescending(ss => ss.Subject!.IsDefault)
                    .ThenBy(ss => ss.Subject!.Name)
                    .Select(ss => new StudentAssignedSubjectBadgeViewModel
                    {
                        SubjectId = ss.SubjectId,
                        SubjectName = ss.Subject!.Name,
                        HourlyRate = ss.HourlyRate,
                        DefaultDurationMinutes = ss.DefaultDurationMinutes,
                        WeeklyTargetCount = ss.WeeklyTargetCount
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return new StudentListFilterViewModel
        {
            SearchTerm = normalizedSearch,
            Status = statusFilter,
            SubjectId = subjectId,
            ActiveCount = activeCount,
            ArchivedCount = archivedCount,
            AvailableSubjects = availableSubjects,
            Students = students
        };
    }

    public async Task<StudentDetailsViewModel?> GetStudentDetailsAsync(
        string teacherId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Öğrenci detaylarına erişmek için öğretmen oturumu gereklidir.");
        }

        var student = await _dbContext.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId && s.TeacherId == teacherId, cancellationToken);

        if (student is null)
        {
            return null;
        }

        var defaultSubjectName = await _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId && !s.IsArchived && s.IsDefault)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var assignedSubjects = await _dbContext.StudentSubjects
            .AsNoTracking()
            .Include(ss => ss.Subject)
            .Where(ss => ss.StudentId == studentId && ss.Student!.TeacherId == teacherId)
            .OrderByDescending(ss => ss.IsActive)
            .ThenByDescending(ss => ss.Subject != null && ss.Subject.IsDefault)
            .ThenBy(ss => ss.Subject != null ? ss.Subject.Name : string.Empty)
            .Select(ss => new StudentSubjectListItemViewModel
            {
                Id = ss.Id,
                StudentId = ss.StudentId,
                SubjectId = ss.SubjectId,
                SubjectName = ss.Subject != null ? ss.Subject.Name : string.Empty,
                IsTeacherDefaultSubject = ss.Subject != null && ss.Subject.IsDefault && !ss.Subject.IsArchived,
                IsSubjectArchived = ss.Subject != null && ss.Subject.IsArchived,
                HourlyRate = ss.HourlyRate,
                DefaultDurationMinutes = ss.DefaultDurationMinutes,
                WeeklyTargetCount = ss.WeeklyTargetCount,
                IsActive = ss.IsActive
            })
            .ToListAsync(cancellationToken);

        var activeItems = assignedSubjects.Where(i => i.IsActive).ToList();

        return new StudentDetailsViewModel
        {
            Id = student.Id,
            FirstName = student.FirstName,
            LastName = student.LastName,
            PhoneNumber = student.PhoneNumber,
            Email = student.Email,
            IsArchived = student.IsArchived,
            Notes = student.Notes,
            CreatedAtUtc = student.CreatedAtUtc,
            UpdatedAtUtc = student.UpdatedAtUtc,
            TeacherDefaultSubjectName = defaultSubjectName,
            ActiveSubjectCount = activeItems.Count,
            InactiveSubjectCount = assignedSubjects.Count - activeItems.Count,
            TotalWeeklySessions = activeItems.Sum(i => i.WeeklyTargetCount),
            TotalWeeklyMinutes = activeItems.Sum(i => i.WeeklyTotalMinutes),
            TotalEstimatedWeeklyFee = activeItems.Sum(i => i.EstimatedWeeklyFee),
            AssignedSubjects = assignedSubjects
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

    public async Task<StudentOperationResult> UpdateStudentNotesAsync(
        string teacherId,
        int studentId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Öğrenci notlarını güncellemek için öğretmen oturumu gereklidir.");
        }

        var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (normalizedNotes is not null && normalizedNotes.Length > 2000)
        {
            return new StudentOperationResult(
                false,
                null,
                new[] { "Özel notlar en fazla 2000 karakter olabilir." });
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

        student.Notes = normalizedNotes;
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
