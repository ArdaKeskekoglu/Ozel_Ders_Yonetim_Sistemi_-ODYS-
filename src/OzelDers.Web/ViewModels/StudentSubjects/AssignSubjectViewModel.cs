using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.StudentSubjects;

public class SubjectOptionViewModel
{
    public int SubjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }

    public bool IsAlreadyAssignedActive { get; set; }
}

public class AssignSubjectViewModel
{
    public int StudentId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lütfen bir ders türü seçiniz.")]
    [Display(Name = "Ders Türü")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Saatlik ders ücreti zorunludur.")]
    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "Saatlik ücret 1 TL ile 1.000.000 TL arasında olmalıdır.")]
    [Display(Name = "Saatlik Ücret (TL / 60 dk)")]
    public decimal HourlyRate { get; set; } = 1000m;

    [Required(ErrorMessage = "Tek oturum süresi zorunludur.")]
    [Range(15, 360, ErrorMessage = "Tek oturum süresi 15 ile 360 dakika arasında olmalıdır.")]
    [Display(Name = "Tek Oturum Süresi (Dakika)")]
    public int DefaultDurationMinutes { get; set; } = 60;

    [Required(ErrorMessage = "Haftalık hedef oturum adedi zorunludur.")]
    [Range(1, 21, ErrorMessage = "Haftalık hedef ders oturumu adedi 1 ile 21 arasında olmalıdır.")]
    [Display(Name = "Haftalık Hedef Oturum Adedi")]
    public int WeeklyTargetCount { get; set; } = 1;

    public int? DefaultSubjectId { get; set; }

    public string? DefaultSubjectName { get; set; }

    public IReadOnlyList<SubjectOptionViewModel> AvailableSubjects { get; set; } =
        Array.Empty<SubjectOptionViewModel>();
}
