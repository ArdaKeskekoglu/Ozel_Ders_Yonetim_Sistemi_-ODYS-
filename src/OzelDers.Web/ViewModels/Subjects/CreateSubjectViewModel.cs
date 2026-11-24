using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Subjects;

/// <summary>
/// Yeni ders türü oluşturma formu ViewModel sınıfı (FR-022, DEC-14, NFR-009).
/// </summary>
public class CreateSubjectViewModel
{
    [Required(ErrorMessage = "Ders türü adı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ders türü adı en az 2, en fazla 100 karakter olmalıdır.")]
    [Display(Name = "Ders Türü Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Bu dersi varsayılan dersim olarak belirle")]
    public bool SetAsDefault { get; set; }
}
