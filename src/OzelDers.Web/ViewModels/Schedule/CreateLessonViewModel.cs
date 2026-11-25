using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Schedule;

/// <summary>
/// Manuel ders ekleme ekranında öğretmenin seçebileceği aktif öğrenci–ders türü (StudentSubject) seçeneği (FR-035..FR-037).
/// Her seçenek tek bir öğrenci ve tek bir ders türü eşleşmesini temsil eder.
/// </summary>
public class StudentSubjectOptionViewModel
{
    public int StudentSubjectId { get; init; }

    public int StudentId { get; init; }

    public required string StudentFullName { get; init; }

    public int SubjectId { get; init; }

    public required string SubjectName { get; init; }

    public bool IsTeacherDefaultSubject { get; init; }

    public decimal HourlyRate { get; init; }

    public int DefaultDurationMinutes { get; init; }

    public int WeeklyTargetCount { get; init; }

    public decimal EstimatedSessionFee => Math.Round(HourlyRate * DefaultDurationMinutes / 60m, 2);

    public string DisplayText =>
        IsTeacherDefaultSubject
            ? $"{StudentFullName} — {SubjectName} (★ Varsayılan • {DefaultDurationMinutes} dk • {HourlyRate:N2} ₺/sa)"
            : $"{StudentFullName} — {SubjectName} ({DefaultDurationMinutes} dk • {HourlyRate:N2} ₺/sa)";
}

/// <summary>
/// Takvim üzerinden manuel veya haftalık tekrarlayan ders oturumu ekleme form modeli (FR-035..FR-038, FR-043, FR-046).
/// </summary>
public class CreateLessonViewModel
{
    [Required(ErrorMessage = "Lütfen bir öğrenci ve ders türü seçiniz.")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen geçerli bir öğrenci ve ders türü seçiniz.")]
    [Display(Name = "Öğrenci ve Ders Türü")]
    public int StudentSubjectId { get; set; }

    [Required(ErrorMessage = "Ders tarihi zorunludur.")]
    [Display(Name = "Başlangıç Ders Tarihi")]
    public DateOnly LessonDate { get; set; }

    [Required(ErrorMessage = "Başlangıç saati zorunludur.")]
    [Display(Name = "Başlangıç Saati")]
    public TimeOnly StartTime { get; set; } = new(15, 0);

    [Required(ErrorMessage = "Ders oturumu süresi zorunludur.")]
    [Range(15, 360, ErrorMessage = "Ders oturumu süresi 15 ile 360 dakika arasında olmalıdır.")]
    [Display(Name = "Oturum Süresi (Dakika)")]
    public int DurationMinutes { get; set; } = 60;

    /// <summary>
    /// Haftalık tekrar sayısı (FR-046): 1 = yalnızca seçili hafta, 2..12 = ardışık N hafta boyunca aynı gün/saatte üretilir.
    /// Her hafta bağımsız bir Lesson kaydı olarak saklandığı için tek hafta istisnası (FR-047) doğal olarak desteklenir.
    /// </summary>
    [Required(ErrorMessage = "Haftalık tekrar sayısı zorunludur.")]
    [Range(1, 12, ErrorMessage = "Haftalık tekrar sayısı 1 ile 12 hafta arasında olmalıdır.")]
    [Display(Name = "Haftalık Tekrar (Hafta Sayısı)")]
    public int RepeatWeeks { get; set; } = 1;

    [MaxLength(1000, ErrorMessage = "Ders notu en fazla 1000 karakter olabilir.")]
    [Display(Name = "Ders Oturumu Notu (İsteğe Bağlı)")]
    public string? Notes { get; set; }

    public int? PreselectedStudentId { get; set; }

    public IReadOnlyList<StudentSubjectOptionViewModel> AvailableEnrollments { get; set; } =
        Array.Empty<StudentSubjectOptionViewModel>();

    public bool HasAvailableEnrollments => AvailableEnrollments.Count > 0;
}
