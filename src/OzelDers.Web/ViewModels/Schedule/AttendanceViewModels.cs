using System.ComponentModel.DataAnnotations;
using OzelDers.Web.Domain.Enums;

namespace OzelDers.Web.ViewModels.Schedule;

/// <summary>
/// Tek bir ders oturumu için yoklama ve gerçekleşme durumu kaydetme girdi modeli (FR-057..FR-068).
/// </summary>
public class RecordLessonAttendanceInputViewModel
{
    [Required]
    public int LessonId { get; set; }

    [Required(ErrorMessage = "Lütfen yoklama ve ders gerçekleşme durumunu seçiniz.")]
    public LessonAttendanceStatus AttendanceStatus { get; set; } = LessonAttendanceStatus.Attended;

    /// <summary>
    /// Öğrenci gelmediğinde (Absent) veya ders iptal edildiğinde öğretmenin ders bazında verdiği ücret kararı (FR-065..FR-067).
    /// Belirtilmezse Absent durumunda otomatik borç oluşmaması için Pending kalır (FR-066, BR-02).
    /// </summary>
    public LessonFeeDecision? FeeDecision { get; set; }

    [StringLength(500, ErrorMessage = "Yoklama notu en fazla 500 karakter olabilir.")]
    public string? AttendanceNote { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public bool ReturnToDetails { get; set; }

    public bool ReturnToPendingCenter { get; set; }
}

/// <summary>
/// Öğrencinin gelmediği veya iptal edilen bir ders için öğretmenin ücret alınıp alınmayacağını
/// belirlediği veya sonradan kontrollü şekilde düzelttiği girdi modeli (FR-065..FR-068).
/// </summary>
public class SetLessonFeeDecisionInputViewModel
{
    [Required]
    public int LessonId { get; set; }

    [Required(ErrorMessage = "Lütfen bu ders için ücret kararını seçiniz.")]
    public LessonFeeDecision FeeDecision { get; set; }

    [StringLength(500, ErrorMessage = "Ücret kararı açıklaması en fazla 500 karakter olabilir.")]
    public string? Reason { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public bool ReturnToDetails { get; set; }

    public bool ReturnToPendingCenter { get; set; }
}

/// <summary>
/// Toplu yoklama doğrulama ekranında tek bir bekleyen ders satırını temsil eder (FR-061, FR-063..FR-067).
/// </summary>
public class PendingAttendanceItemViewModel
{
    public int LessonId { get; init; }

    public int StudentSubjectId { get; init; }

    public int StudentId { get; init; }

    public required string StudentFullName { get; init; }

    public int SubjectId { get; init; }

    public required string SubjectName { get; init; }

    public DateOnly LessonDate { get; init; }

    public required string DayNameTr { get; init; }

    public DateTime StartUtc { get; init; }

    public DateTime EndUtc { get; init; }

    public DateTime StartLocal { get; init; }

    public DateTime EndLocal { get; init; }

    public int DurationMinutes { get; init; }

    public decimal HourlyRateSnapshot { get; init; }

    public decimal SessionFeeSnapshot { get; init; }

    public LessonStatus Status { get; init; }

    public LessonAttendanceStatus AttendanceStatus { get; init; }

    public LessonFeeDecision FeeDecision { get; init; }

    public string? Notes { get; init; }

    public string TimeRangeText => $"{StartLocal:HH:mm} – {EndLocal:HH:mm}";
}

/// <summary>
/// Gün sonunda veya haftalık takvimde yoklama doğrulaması ve devamsızlık ücret kararı bekleyen derslerin
/// topluca görüntülendiği ekran görünüm modeli (FR-060..FR-068).
/// </summary>
public class PendingAttendanceListViewModel
{
    public DateOnly? FilterWeekStartDate { get; init; }

    public string? FilterWeekRangeTitle { get; init; }

    public int TotalPendingCount => Items.Count;

    public int TotalPendingMinutes => Items.Sum(i => i.DurationMinutes);

    public decimal TotalPendingFeeSnapshot => Items.Sum(i => i.SessionFeeSnapshot);

    public int PendingFeeDecisionCount => PendingFeeDecisionItems.Count;

    public IReadOnlyList<PendingAttendanceItemViewModel> Items { get; init; } =
        Array.Empty<PendingAttendanceItemViewModel>();

    /// <summary>
    /// Öğrenci gelmedi (Absent) olarak işaretlenmiş ancak öğretmenin henüz ücret kararını (Charge / Waive)
    /// tamamlamadığı dersler (BR-02, BR-03, FR-065, FR-066).
    /// </summary>
    public IReadOnlyList<PendingAttendanceItemViewModel> PendingFeeDecisionItems { get; init; } =
        Array.Empty<PendingAttendanceItemViewModel>();
}

/// <summary>
/// Birden fazla geçmiş dersin yoklamasını tek işlemde topluca tamamlamak için kullanılan girdi modeli (FR-064).
/// </summary>
public class BatchRecordAttendanceInputViewModel
{
    public List<int> LessonIds { get; set; } = new();

    public LessonAttendanceStatus AttendanceStatus { get; set; } = LessonAttendanceStatus.Attended;

    public LessonFeeDecision? FeeDecision { get; set; }

    [StringLength(500, ErrorMessage = "Toplu yoklama notu en fazla 500 karakter olabilir.")]
    public string? AttendanceNote { get; set; }

    public DateOnly? FilterDate { get; set; }
}
