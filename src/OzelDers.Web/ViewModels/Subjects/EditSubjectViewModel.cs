using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Subjects;

/// <summary>
/// Mevcut ders türünü düzenleme formu ViewModel sınıfı (FR-023, NFR-009).
/// </summary>
public class EditSubjectViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ders türü adı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ders türü adı en az 2, en fazla 100 karakter olmalıdır.")]
    [Display(Name = "Ders Türü Adı")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Bu dersi varsayılan dersim olarak belirle")]
    public bool IsDefault { get; set; }

    public bool IsArchived { get; set; }
}
