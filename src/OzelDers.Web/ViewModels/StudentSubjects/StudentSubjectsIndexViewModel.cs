namespace OzelDers.Web.ViewModels.StudentSubjects;

public class StudentSubjectsIndexViewModel
{
    public int StudentId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public bool IsStudentArchived { get; set; }

    public string? TeacherDefaultSubjectName { get; set; }

    public int ActiveCount { get; set; }

    public int InactiveCount { get; set; }

    public int TotalWeeklySessions { get; set; }

    public int TotalWeeklyMinutes { get; set; }

    public decimal TotalEstimatedWeeklyFee { get; set; }

    public IReadOnlyList<StudentSubjectListItemViewModel> Items { get; set; } =
        Array.Empty<StudentSubjectListItemViewModel>();

    public bool HasAnyAssignments => Items.Count > 0;
}
