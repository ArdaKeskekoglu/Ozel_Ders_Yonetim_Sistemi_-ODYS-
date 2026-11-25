using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.StudentSubjects;

public class EditStudentSubjectViewModel
{
    public int Id { get; set; }

    public int StudentId { get; set; }

    public string StudentFullName { get; set; } = string.Empty;

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Saatlik ders ücreti zorunludur.")]
    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "Saatlik ücret 1 TL ile 1.000.000 TL arasında olmalıdır.")]
    [Display(Name = "Saatlik Ücret (TL / 60 dk)")]
    public decimal HourlyRate { get; set; }

    [Required(ErrorMessage = "Tek oturum süresi zorunludur.")]
    [Range(15, 360, ErrorMessage = "Tek oturum süresi 15 ile 360 dakika arasında olmalıdır.")]
    [Display(Name = "Tek Oturum Süresi (Dakika)")]
    public int DefaultDurationMinutes { get; set; } = 60;

    [Required(ErrorMessage = "Haftalık hedef oturum adedi zorunludur.")]
    [Range(1, 21, ErrorMessage = "Haftalık hedef ders oturumu adedi 1 ile 21 arasında olmalıdır.")]
    [Display(Name = "Haftalık Hedef Oturum Adedi")]
    public int WeeklyTargetCount { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
