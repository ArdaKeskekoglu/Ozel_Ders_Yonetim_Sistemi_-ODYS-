using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.StudentSubjects;

namespace OzelDers.Web.Services;

/// <summary>
/// Öğrenci–ders türü ve ücret yönetimi servisi (FR-025..FR-030, BR-16, DEC-14, NFR-007).
/// - Öğrenciye ders atanırken öğretmenin varsayılan dersi (IsDefault = true) otomatik seçili gelir.
/// - Bir öğrenciye birden fazla ders türü farklı saatlik ücret, oturum süresi ve haftalık oturum adediyle atanabilir.
/// - Yalnızca ilgili öğretmene ait öğrenci ve aktif ders türleri eşleştirilebilir (veri izolasyonu).
/// </summary>
public class StudentSubjectService : IStudentSubjectService
{
    private readonly ApplicationDbContext _dbContext;

    public StudentSubjectService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StudentSubjectsIndexViewModel?> GetStudentSubjectsAsync(
        string teacherId,
        int studentId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return null;
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

        var items = await _dbContext.StudentSubjects
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

        var activeItems = items.Where(i => i.IsActive).ToList();

        return new StudentSubjectsIndexViewModel
        {
            StudentId = student.Id,
            StudentFullName = student.FullName,
            IsStudentArchived = student.IsArchived,
            TeacherDefaultSubjectName = defaultSubjectName,
            ActiveCount = activeItems.Count,
            InactiveCount = items.Count - activeItems.Count,
            TotalWeeklySessions = activeItems.Sum(i => i.WeeklyTargetCount),
            TotalWeeklyMinutes = activeItems.Sum(i => i.WeeklyTotalMinutes),
            TotalEstimatedWeeklyFee = activeItems.Sum(i => i.EstimatedWeeklyFee),
            Items = items
        };
    }

    public async Task<AssignSubjectViewModel?> PrepareAssignViewModelAsync(
        string teacherId,
        int studentId,
        AssignSubjectViewModel? existingModel = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return null;
        }

        var student = await _dbContext.Students
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == studentId && s.TeacherId == teacherId, cancellationToken);

        if (student is null)
        {
            return null;
        }

        var activeSubjects = await _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId && !s.IsArchived)
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);

        var assignedActiveSubjectIds = await _dbContext.StudentSubjects
            .AsNoTracking()
            .Where(ss => ss.StudentId == studentId && ss.IsActive)
            .Select(ss => ss.SubjectId)
            .ToListAsync(cancellationToken);

        var assignedSet = assignedActiveSubjectIds.ToHashSet();
        var defaultSubject = activeSubjects.FirstOrDefault(s => s.IsDefault)
                             ?? activeSubjects.FirstOrDefault();

        var options = activeSubjects
            .Select(s => new SubjectOptionViewModel
            {
                SubjectId = s.Id,
                Name = s.Name,
                IsDefault = s.IsDefault,
                IsAlreadyAssignedActive = assignedSet.Contains(s.Id)
            })
            .ToList();

        var viewModel = existingModel ?? new AssignSubjectViewModel();
        viewModel.StudentId = student.Id;
        viewModel.StudentFullName = student.FullName;
        viewModel.DefaultSubjectId = defaultSubject?.Id;
        viewModel.DefaultSubjectName = defaultSubject?.Name;
        viewModel.AvailableSubjects = options;

        // FR-025: Öğrenciye ders atanırken öğretmenin varsayılan dersi otomatik seçili gelir.
        if (existingModel is null || viewModel.SubjectId <= 0)
        {
            viewModel.SubjectId = defaultSubject?.Id ?? 0;
        }

        return viewModel;
    }

    public async Task<StudentSubjectOperationResult> AssignSubjectToStudentAsync(
        string teacherId,
        AssignSubjectViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Fail("Öğretmen kimliği doğrulanamadı.");
        }

        var validationErrors = ValidatePricingAndSchedule(
            model.HourlyRate,
            model.DefaultDurationMinutes,
            model.WeeklyTargetCount);

        if (validationErrors.Count > 0)
        {
            return new StudentSubjectOperationResult(false, null, null, validationErrors);
        }

        var student = await _dbContext.Students
            .FirstOrDefaultAsync(s => s.Id == model.StudentId && s.TeacherId == teacherId, cancellationToken);

        if (student is null)
        {
            return Fail("Öğrenci bulunamadı veya bu öğrenci üzerinde işlem yetkiniz yok.");
        }

        var subject = await _dbContext.Subjects
            .FirstOrDefaultAsync(
                s => s.Id == model.SubjectId && s.TeacherId == teacherId && !s.IsArchived,
                cancellationToken);

        if (subject is null)
        {
            return Fail("Seçilen ders türü bulunamadı, arşivlenmiş veya hesabınıza ait değil.");
        }

        var existingAssignment = await _dbContext.StudentSubjects
            .FirstOrDefaultAsync(
                ss => ss.StudentId == student.Id && ss.SubjectId == subject.Id,
                cancellationToken);

        if (existingAssignment is not null)
        {
            if (existingAssignment.IsActive)
            {
                return Fail($"'{subject.Name}' ders türü {student.FullName} adlı öğrenciye zaten aktif olarak atanmış. Ücret veya haftalık hedefini düzenleme ekranından güncelleyebilirsiniz.");
            }

            // Daha önce pasife alınmış bir öğrenci-ders kaydı varsa geçmiş ders bağlantısını koruyarak yeniden aktifleştir
            existingAssignment.IsActive = true;
            existingAssignment.HourlyRate = decimal.Round(model.HourlyRate, 2);
            existingAssignment.DefaultDurationMinutes = model.DefaultDurationMinutes;
            existingAssignment.WeeklyTargetCount = model.WeeklyTargetCount;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new StudentSubjectOperationResult(
                true,
                existingAssignment,
                $"'{subject.Name}' ders türü {student.FullName} için güncellenerek yeniden aktifleştirildi.",
                Array.Empty<string>());
        }

        var studentSubject = new StudentSubject
        {
            StudentId = student.Id,
            SubjectId = subject.Id,
            HourlyRate = decimal.Round(model.HourlyRate, 2),
            DefaultDurationMinutes = model.DefaultDurationMinutes,
            WeeklyTargetCount = model.WeeklyTargetCount,
            IsActive = true
        };

        _dbContext.StudentSubjects.Add(studentSubject);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StudentSubjectOperationResult(
            true,
            studentSubject,
            $"'{subject.Name}' ders türü {student.FullName} adlı öğrenciye başarıyla atandı.",
            Array.Empty<string>());
    }

    public async Task<EditStudentSubjectViewModel?> GetForEditAsync(
        string teacherId,
        int studentSubjectId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return null;
        }

        var assignment = await _dbContext.StudentSubjects
            .AsNoTracking()
            .Include(ss => ss.Student)
            .Include(ss => ss.Subject)
            .FirstOrDefaultAsync(
                ss => ss.Id == studentSubjectId &&
                      ss.Student != null &&
                      ss.Student.TeacherId == teacherId &&
                      ss.Subject != null &&
                      ss.Subject.TeacherId == teacherId,
                cancellationToken);

        if (assignment is null || assignment.Student is null || assignment.Subject is null)
        {
            return null;
        }

        return new EditStudentSubjectViewModel
        {
            Id = assignment.Id,
            StudentId = assignment.StudentId,
            StudentFullName = assignment.Student.FullName,
            SubjectId = assignment.SubjectId,
            SubjectName = assignment.Subject.Name,
            HourlyRate = assignment.HourlyRate,
            DefaultDurationMinutes = assignment.DefaultDurationMinutes,
            WeeklyTargetCount = assignment.WeeklyTargetCount,
            IsActive = assignment.IsActive
        };
    }

    public async Task<StudentSubjectOperationResult> UpdateStudentSubjectAsync(
        string teacherId,
        EditStudentSubjectViewModel model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Fail("Öğretmen kimliği doğrulanamadı.");
        }

        var validationErrors = ValidatePricingAndSchedule(
            model.HourlyRate,
            model.DefaultDurationMinutes,
            model.WeeklyTargetCount);

        if (validationErrors.Count > 0)
        {
            return new StudentSubjectOperationResult(false, null, null, validationErrors);
        }

        var assignment = await _dbContext.StudentSubjects
            .Include(ss => ss.Student)
            .Include(ss => ss.Subject)
            .FirstOrDefaultAsync(
                ss => ss.Id == model.Id &&
                      ss.Student != null &&
                      ss.Student.TeacherId == teacherId &&
                      ss.Subject != null &&
                      ss.Subject.TeacherId == teacherId,
                cancellationToken);

        if (assignment is null || assignment.Student is null || assignment.Subject is null)
        {
            return Fail("Öğrenci–ders türü kaydı bulunamadı veya yetkiniz yok.");
        }

        assignment.HourlyRate = decimal.Round(model.HourlyRate, 2);
        assignment.DefaultDurationMinutes = model.DefaultDurationMinutes;
        assignment.WeeklyTargetCount = model.WeeklyTargetCount;
        assignment.IsActive = model.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new StudentSubjectOperationResult(
            true,
            assignment,
            $"{assignment.Student.FullName} — '{assignment.Subject.Name}' ders ücreti ve haftalık hedef bilgileri güncellendi. Geçmiş ders kayıtları bu değişiklikten etkilenmez.",
            Array.Empty<string>());
    }

    public async Task<StudentSubjectOperationResult> SetActiveStatusAsync(
        string teacherId,
        int studentSubjectId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            return Fail("Öğretmen kimliği doğrulanamadı.");
        }

        var assignment = await _dbContext.StudentSubjects
            .Include(ss => ss.Student)
            .Include(ss => ss.Subject)
            .FirstOrDefaultAsync(
                ss => ss.Id == studentSubjectId &&
                      ss.Student != null &&
                      ss.Student.TeacherId == teacherId &&
                      ss.Subject != null &&
                      ss.Subject.TeacherId == teacherId,
                cancellationToken);

        if (assignment is null || assignment.Student is null || assignment.Subject is null)
        {
            return Fail("Öğrenci–ders türü kaydı bulunamadı veya yetkiniz yok.");
        }

        if (isActive && assignment.Subject.IsArchived)
        {
            return Fail($"'{assignment.Subject.Name}' ders türü arşivlenmiş durumda. Bu atamayı aktifleştirmeden önce Ders Türleri sayfasından ilgili ders türünü yeniden aktif ediniz.");
        }

        assignment.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var statusText = isActive
            ? $"'{assignment.Subject.Name}' dersi {assignment.Student.FullName} için yeniden aktifleştirildi."
            : $"'{assignment.Subject.Name}' dersi {assignment.Student.FullName} için pasife alındı. Geçmiş ders kayıtları korundu.";

        return new StudentSubjectOperationResult(true, assignment, statusText, Array.Empty<string>());
    }

    private static List<string> ValidatePricingAndSchedule(
        decimal hourlyRate,
        int defaultDurationMinutes,
        int weeklyTargetCount)
    {
        var errors = new List<string>();

        if (hourlyRate < 1m || hourlyRate > 1_000_000m)
        {
            errors.Add("Saatlik ücret 1 TL ile 1.000.000 TL arasında olmalıdır.");
        }

        if (defaultDurationMinutes < 15 || defaultDurationMinutes > 360)
        {
            errors.Add("Tek oturum süresi 15 ile 360 dakika arasında olmalıdır.");
        }

        if (weeklyTargetCount < 1 || weeklyTargetCount > 21)
        {
            errors.Add("Haftalık hedef ders oturumu adedi 1 ile 21 arasında olmalıdır.");
        }

        return errors;
    }

    private static StudentSubjectOperationResult Fail(string error) =>
        new(false, null, null, new[] { error });
}
