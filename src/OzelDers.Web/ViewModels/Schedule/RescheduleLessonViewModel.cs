using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Schedule;

/// <summary>
/// Ders oturumunu başka bir gün veya saate erteleme ve erteleme geçmiş izi bırakma form modeli (FR-040, FR-041, FR-045, FR-059).
/// </summary>
public class RescheduleLessonViewModel
{
    public int LessonId { get; set; }

    public int StudentId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public DateOnly CurrentDate { get; set; }

    public TimeOnly CurrentStartTime { get; set; }

    public TimeOnly CurrentEndTime { get; set; }

    public int CurrentDurationMinutes { get; set; }

    public decimal HourlyRateSnapshot { get; set; }

    public decimal SessionFeeSnapshot { get; set; }

    [Required(ErrorMessage = "Yeni ders tarihi zorunludur.")]
    [Display(Name = "Yeni Ders Tarihi")]
    public DateOnly NewDate { get; set; }

    [Required(ErrorMessage = "Yeni başlangıç saati zorunludur.")]
    [Display(Name = "Yeni Başlangıç Saati")]
    public TimeOnly NewStartTime { get; set; }

    [Required(ErrorMessage = "Ders oturumu süresi zorunludur.")]
    [Range(15, 360, ErrorMessage = "Ders oturumu süresi 15 ile 360 dakika arasında olmalıdır.")]
    [Display(Name = "Yeni Oturum Süresi (Dakika)")]
    public int NewDurationMinutes { get; set; } = 60;

    [MaxLength(500, ErrorMessage = "Erteleme açıklaması en fazla 500 karakter olabilir.")]
    [Display(Name = "Erteleme Nedeni / Notu (İsteğe Bağlı)")]
    public string? Reason { get; set; }
}
