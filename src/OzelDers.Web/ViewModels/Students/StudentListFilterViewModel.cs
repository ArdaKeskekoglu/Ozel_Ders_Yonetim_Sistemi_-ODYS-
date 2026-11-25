namespace OzelDers.Web.ViewModels.Students;

public enum StudentStatusFilter
{
    Active = 0,
    Archived = 1,
    All = 2
}

public class StudentSubjectFilterOptionViewModel
{
    public int Id { get; init; }
    public required string Name { get; init; }
}

/// <summary>
/// Öğrenci listesi, arama, durum/ders türü filtreleme ve boş durum bilgilerini taşıyan ekran modeli (FR-013..FR-015).
/// </summary>
public class StudentListFilterViewModel
{
    public string? SearchTerm { get; init; }
    public StudentStatusFilter Status { get; init; } = StudentStatusFilter.Active;
    public int? SubjectId { get; init; }
    public int ActiveCount { get; init; }
    public int ArchivedCount { get; init; }
    public int TotalCount => ActiveCount + ArchivedCount;
    public IReadOnlyList<StudentSubjectFilterOptionViewModel> AvailableSubjects { get; init; } =
        Array.Empty<StudentSubjectFilterOptionViewModel>();
    public IReadOnlyList<StudentListItemViewModel> Students { get; init; } = Array.Empty<StudentListItemViewModel>();
    public bool HasAnyStudents => TotalCount > 0;
    public bool HasFilteredResults => Students.Count > 0;
}
