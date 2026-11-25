namespace OzelDers.Web.ViewModels.StudentSubjects;

public class StudentSubjectListItemViewModel
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public bool IsTeacherDefaultSubject { get; set; }

    public bool IsSubjectArchived { get; set; }

    public decimal HourlyRate { get; set; }

    public int DefaultDurationMinutes { get; set; }

    public int WeeklyTargetCount { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// Saatlik ücret ve tek oturum süresine göre tek bir ders oturumunun ücreti.
    /// </summary>
    public decimal EstimatedSessionFee => Math.Round(HourlyRate * DefaultDurationMinutes / 60m, 2);

    /// <summary>
    /// Haftalık toplam hedef ders süresi (dakika).
    /// </summary>
    public int WeeklyTotalMinutes => WeeklyTargetCount * DefaultDurationMinutes;

    /// <summary>
    /// Haftalık hedef oturum sayısına göre tahmini haftalık toplam ücret.
    /// </summary>
    public decimal EstimatedWeeklyFee => Math.Round(EstimatedSessionFee * WeeklyTargetCount, 2);
}
