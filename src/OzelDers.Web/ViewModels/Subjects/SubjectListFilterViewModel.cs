namespace OzelDers.Web.ViewModels.Subjects;

public enum SubjectStatusFilter
{
    Active = 0,
    Archived = 1,
    All = 2
}

/// <summary>
/// Öğretmenin ders türleri listesi, durum filtresi ve varsayılan ders özetini taşıyan model (FR-022..FR-024).
/// </summary>
public class SubjectListFilterViewModel
{
    public SubjectStatusFilter Status { get; init; } = SubjectStatusFilter.Active;
    public int ActiveCount { get; init; }
    public int ArchivedCount { get; init; }
    public int TotalCount => ActiveCount + ArchivedCount;
    public string? DefaultSubjectName { get; init; }
    public IReadOnlyList<SubjectListItemViewModel> Subjects { get; init; } = Array.Empty<SubjectListItemViewModel>();
    public bool HasAnySubjects => TotalCount > 0;
    public bool HasFilteredResults => Subjects.Count > 0;
    public bool CanArchiveActiveSubject => ActiveCount > 1;
}
