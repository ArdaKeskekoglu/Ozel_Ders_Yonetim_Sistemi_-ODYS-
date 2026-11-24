using Microsoft.EntityFrameworkCore;
using OzelDers.Web.Data;
using OzelDers.Web.Domain.Entities;
using OzelDers.Web.Services.Abstractions;
using OzelDers.Web.ViewModels.Subjects;

namespace OzelDers.Web.Services;

/// <summary>
/// Yalnızca oturumdaki öğretmenin kendi ders türlerini yöneten, varsayılan ders
/// bütünlüğünü (en az 1 aktif ders varken tam olarak 1 aktif varsayılan ders) ve
/// arşivlenmiş ders adlarının yeniden etkinleştirilmesini sağlayan servis
/// (FR-022..FR-024, BR-16, BR-19, NFR-007, NFR-008).
/// </summary>
public class SubjectService : ISubjectService
{
    private readonly ApplicationDbContext _dbContext;

    public SubjectService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SubjectListFilterViewModel> GetSubjectsForTeacherAsync(
        string teacherId,
        SubjectStatusFilter statusFilter = SubjectStatusFilter.Active,
        CancellationToken cancellationToken = default)
    {
        EnsureTeacherId(teacherId);

        await EnsureDefaultSubjectInvariantAsync(teacherId, cancellationToken);

        var teacherSubjectsQuery = _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.TeacherId == teacherId);

        var activeCount = await teacherSubjectsQuery.CountAsync(s => !s.IsArchived, cancellationToken);
        var archivedCount = await teacherSubjectsQuery.CountAsync(s => s.IsArchived, cancellationToken);

        var defaultSubjectName = await teacherSubjectsQuery
            .Where(s => !s.IsArchived && s.IsDefault)
            .Select(s => s.Name)
            .FirstOrDefaultAsync(cancellationToken);

        var filteredQuery = statusFilter switch
        {
            SubjectStatusFilter.Active => teacherSubjectsQuery.Where(s => !s.IsArchived),
            SubjectStatusFilter.Archived => teacherSubjectsQuery.Where(s => s.IsArchived),
            _ => teacherSubjectsQuery
        };

        var items = await filteredQuery
            .OrderBy(s => s.IsArchived)
            .ThenByDescending(s => s.IsDefault)
            .ThenBy(s => s.Name)
            .Select(s => new SubjectListItemViewModel
            {
                Id = s.Id,
                Name = s.Name,
                IsDefault = s.IsDefault,
                IsArchived = s.IsArchived,
                CreatedAtUtc = s.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new SubjectListFilterViewModel
        {
            Status = statusFilter,
            ActiveCount = activeCount,
            ArchivedCount = archivedCount,
            DefaultSubjectName = defaultSubjectName,
            Subjects = items
        };
    }

    public async Task<SubjectOperationResult> CreateSubjectAsync(
        string teacherId,
        CreateSubjectViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        EnsureTeacherId(teacherId);

        var normalizedName = model.Name?.Trim() ?? string.Empty;
        if (normalizedName.Length < 2 || normalizedName.Length > 100)
        {
            return new SubjectOperationResult(
                false,
                null,
                null,
                new[] { "Ders türü adı en az 2, en fazla 100 karakter olmalıdır." });
        }

        var existingSubjects = await _dbContext.Subjects
            .Where(s => s.TeacherId == teacherId)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var matchingByName = existingSubjects.FirstOrDefault(s =>
            string.Equals(s.Name, normalizedName, StringComparison.OrdinalIgnoreCase));

        var activeSubjects = existingSubjects.Where(s => !s.IsArchived).ToList();
        var shouldBeDefault = model.SetAsDefault || activeSubjects.Count == 0 || !activeSubjects.Any(s => s.IsDefault);

        if (matchingByName is not null)
        {
            if (!matchingByName.IsArchived)
            {
                return new SubjectOperationResult(
                    false,
                    null,
                    null,
                    new[] { "Bu isimde aktif bir ders türünüz zaten mevcut." });
            }

            // Arşivlenmiş aynı isimli ders varsa yeni mükerrer satır açmak yerine mevcut kaydı yeniden etkinleştir
            matchingByName.Name = normalizedName;
            matchingByName.IsArchived = false;

            if (shouldBeDefault)
            {
                await ClearCurrentDefaultSubjectsAsync(teacherId, matchingByName.Id, cancellationToken);
                matchingByName.IsDefault = true;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            return new SubjectOperationResult(
                true,
                matchingByName,
                $"Arşivde bulunan '{matchingByName.Name}' ders türü yeniden etkinleştirildi.",
                Array.Empty<string>());
        }

        if (shouldBeDefault)
        {
            await ClearCurrentDefaultSubjectsAsync(teacherId, excludeSubjectId: null, cancellationToken);
        }

        var newSubject = new Subject
        {
            TeacherId = teacherId,
            Name = normalizedName,
            IsDefault = shouldBeDefault,
            IsArchived = false
        };

        _dbContext.Subjects.Add(newSubject);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var info = newSubject.IsDefault
            ? $"'{newSubject.Name}' ders türü eklendi ve varsayılan ders olarak belirlendi."
            : $"'{newSubject.Name}' ders türü başarıyla eklendi.";

        return new SubjectOperationResult(true, newSubject, info, Array.Empty<string>());
    }

    public async Task<EditSubjectViewModel?> GetSubjectForEditAsync(
        string teacherId,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        EnsureTeacherId(teacherId);

        return await _dbContext.Subjects
            .AsNoTracking()
            .Where(s => s.Id == subjectId && s.TeacherId == teacherId)
            .Select(s => new EditSubjectViewModel
            {
                Id = s.Id,
                Name = s.Name,
                IsDefault = s.IsDefault,
                IsArchived = s.IsArchived
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SubjectOperationResult> UpdateSubjectAsync(
        string teacherId,
        EditSubjectViewModel model,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(model);
        EnsureTeacherId(teacherId);

        var subject = await _dbContext.Subjects
            .FirstOrDefaultAsync(s => s.Id == model.Id && s.TeacherId == teacherId, cancellationToken);

        if (subject is null)
        {
            return new SubjectOperationResult(
                false,
                null,
                null,
                new[] { "Ders türü bulunamadı veya bu kaydı düzenleme yetkiniz yok." });
        }

        var normalizedName = model.Name?.Trim() ?? string.Empty;
        if (normalizedName.Length < 2 || normalizedName.Length > 100)
        {
            return new SubjectOperationResult(
                false,
                null,
                null,
                new[] { "Ders türü adı en az 2, en fazla 100 karakter olmalıdır." });
        }

        var duplicateExists = await _dbContext.Subjects
            .AnyAsync(
                s => s.TeacherId == teacherId &&
                     s.Id != subject.Id &&
                     s.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken);

        if (duplicateExists)
        {
            return new SubjectOperationResult(
                false,
                null,
                null,
                new[] { "Bu isimde başka bir ders türünüz zaten mevcut." });
        }

        subject.Name = normalizedName;

        if (!subject.IsArchived && model.IsDefault && !subject.IsDefault)
        {
            await ClearCurrentDefaultSubjectsAsync(teacherId, subject.Id, cancellationToken);
            subject.IsDefault = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await EnsureDefaultSubjectInvariantAsync(teacherId, cancellationToken);

        return new SubjectOperationResult(
            true,
            subject,
            $"'{subject.Name}' ders türü güncellendi.",
            Array.Empty<string>());
    }

    public async Task<SubjectOperationResult> SetDefaultSubjectAsync(
        string teacherId,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        EnsureTeacherId(teacherId);

        var subject = await _dbContext.Subjects
            .FirstOrDefaultAsync(s => s.Id == subjectId && s.TeacherId == teacherId, cancellationToken);

        if (subject is null)
        {
            return new SubjectOperationResult(
                false,
                null,
                null,
                new[] { "Ders türü bulunamadı veya bu işlem için yetkiniz yok." });
        }

        if (subject.IsArchived)
        {
            return new SubjectOperationResult(
                false,
                null,
                null,
                new[] { "Arşivlenmiş bir ders türü varsayılan olarak belirlenemez. Önce dersi yeniden aktif ediniz." });
        }

        if (!subject.IsDefault)
        {
            await ClearCurrentDefaultSubjectsAsync(teacherId, subject.Id, cancellationToken);
            subject.IsDefault = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return new SubjectOperationResult(
            true,
            subject,
            $"'{subject.Name}' yeni varsayılan ders türünüz olarak belirlendi.",
            Array.Empty<string>());
    }

    public async Task<SubjectOperationResult> SetSubjectArchiveStatusAsync(
        string teacherId,
        int subjectId,
        bool isArchived,
        CancellationToken cancellationToken = default)
    {
        EnsureTeacherId(teacherId);

        var teacherSubjects = await _dbContext.Subjects
            .Where(s => s.TeacherId == teacherId)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        var target = teacherSubjects.FirstOrDefault(s => s.Id == subjectId);
        if (target is null)
        {
            return new SubjectOperationResult(
                false,
                null,
                null,
                new[] { "Ders türü bulunamadı veya bu işlem için yetkiniz yok." });
        }

        if (isArchived)
        {
            if (target.IsArchived)
            {
                return new SubjectOperationResult(true, target, $"'{target.Name}' zaten arşivde.", Array.Empty<string>());
            }

            var remainingActive = teacherSubjects
                .Where(s => !s.IsArchived && s.Id != target.Id)
                .OrderBy(s => s.Id)
                .ToList();

            if (remainingActive.Count == 0)
            {
                return new SubjectOperationResult(
                    false,
                    null,
                    null,
                    new[] { "En az bir aktif ders türünüz bulunmalıdır. Son aktif dersinizi arşivlemeden önce yeni bir ders türü ekleyiniz." });
            }

            var wasDefault = target.IsDefault;
            target.IsArchived = true;
            target.IsDefault = false;

            string infoMessage;
            if (wasDefault || !remainingActive.Any(s => s.IsDefault))
            {
                // Filtered unique index ihlalini önlemek için önce eski varsayılanı kaldırıp kaydediyoruz, ardından yeni varsayılanı atıyoruz
                await _dbContext.SaveChangesAsync(cancellationToken);

                var newDefault = remainingActive[0];
                newDefault.IsDefault = true;
                await _dbContext.SaveChangesAsync(cancellationToken);

                infoMessage = $"'{target.Name}' arşivlendi. '{newDefault.Name}' yeni varsayılan ders olarak belirlendi.";
            }
            else
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
                infoMessage = $"'{target.Name}' ders türü arşivlendi. Geçmiş bağlantılar korundu.";
            }

            return new SubjectOperationResult(true, target, infoMessage, Array.Empty<string>());
        }
        else
        {
            var hasActiveDefault = teacherSubjects.Any(s => !s.IsArchived && s.IsDefault && s.Id != target.Id);
            target.IsArchived = false;
            if (!hasActiveDefault)
            {
                target.IsDefault = true;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);

            var infoMessage = target.IsDefault
                ? $"'{target.Name}' yeniden aktif edildi ve varsayılan ders olarak belirlendi."
                : $"'{target.Name}' ders türü yeniden aktif hâle getirildi.";

            return new SubjectOperationResult(true, target, infoMessage, Array.Empty<string>());
        }
    }

    private async Task ClearCurrentDefaultSubjectsAsync(
        string teacherId,
        int? excludeSubjectId,
        CancellationToken cancellationToken)
    {
        var currentDefaults = await _dbContext.Subjects
            .Where(s => s.TeacherId == teacherId && s.IsDefault && (!excludeSubjectId.HasValue || s.Id != excludeSubjectId.Value))
            .ToListAsync(cancellationToken);

        if (currentDefaults.Count == 0)
        {
            return;
        }

        foreach (var item in currentDefaults)
        {
            item.IsDefault = false;
        }

        // Filtered unique index (WHERE IsDefault = 1 AND IsArchived = 0) nedeniyle önce eski varsayılanları sıfırla
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureDefaultSubjectInvariantAsync(
        string teacherId,
        CancellationToken cancellationToken)
    {
        var activeSubjects = await _dbContext.Subjects
            .Where(s => s.TeacherId == teacherId && !s.IsArchived)
            .OrderBy(s => s.Id)
            .ToListAsync(cancellationToken);

        if (activeSubjects.Count == 0)
        {
            return;
        }

        var defaults = activeSubjects.Where(s => s.IsDefault).ToList();
        if (defaults.Count == 1)
        {
            return;
        }

        if (defaults.Count == 0)
        {
            activeSubjects[0].IsDefault = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        for (var i = 1; i < defaults.Count; i++)
        {
            defaults[i].IsDefault = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureTeacherId(string teacherId)
    {
        if (string.IsNullOrWhiteSpace(teacherId))
        {
            throw new UnauthorizedAccessException("Ders türlerine erişmek için öğretmen oturumu gereklidir.");
        }
    }
}
