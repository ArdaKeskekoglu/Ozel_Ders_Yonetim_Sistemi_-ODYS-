using System.ComponentModel.DataAnnotations;
using OzelDers.Web.Domain.Enums;

namespace OzelDers.Web.ViewModels.Schedule;

/// <summary>
/// Takvimdeki mevcut bir ders oturumunu düzenleme ve başka bir gün/saate taşıma form modeli (FR-039, FR-040, FR-043).
/// </summary>
public class EditLessonViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Lütfen bir öğrenci ve ders türü seçiniz.")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen geçerli bir öğrenci ve ders türü seçiniz.")]
    [Display(Name = "Öğrenci ve Ders Türü")]
    public int StudentSubjectId { get; set; }

    [Required(ErrorMessage = "Ders tarihi zorunludur.")]
    [Display(Name = "Ders Tarihi")]
    public DateOnly LessonDate { get; set; }

    [Required(ErrorMessage = "Başlangıç saati zorunludur.")]
    [Display(Name = "Başlangıç Saati")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Ders oturumu süresi zorunludur.")]
    [Range(15, 360, ErrorMessage = "Ders oturumu süresi 15 ile 360 dakika arasında olmalıdır.")]
    [Display(Name = "Oturum Süresi (Dakika)")]
    public int DurationMinutes { get; set; } = 60;

    [MaxLength(1000, ErrorMessage = "Ders notu en fazla 1000 karakter olabilir.")]
    [Display(Name = "Ders Oturumu Notu (İsteğe Bağlı)")]
    public string? Notes { get; set; }

    public decimal HourlyRateSnapshot { get; set; }

    public decimal SessionFeeSnapshot { get; set; }

    public LessonStatus Status { get; set; }

    public IReadOnlyList<StudentSubjectOptionViewModel> AvailableEnrollments { get; set; } =
        Array.Empty<StudentSubjectOptionViewModel>();
}
