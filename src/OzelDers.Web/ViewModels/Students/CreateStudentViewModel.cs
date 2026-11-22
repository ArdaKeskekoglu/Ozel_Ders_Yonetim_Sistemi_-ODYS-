using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Students;

/// <summary>
/// Yeni öğrenci ekleme formu ViewModel sınıfı (FR-010, FR-011, FR-021, DEC-13, NFR-009).
/// </summary>
public class CreateStudentViewModel
{
    [Required(ErrorMessage = "Öğrenci adı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Öğrenci adı en az 2, en fazla 100 karakter olmalıdır.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Öğrenci soyadı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Öğrenci soyadı en az 2, en fazla 100 karakter olmalıdır.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Lütfen geçerli bir telefon numarası giriniz.")]
    [StringLength(30, ErrorMessage = "Telefon numarası en fazla 30 karakter olabilir.")]
    [Display(Name = "Telefon Numarası (İsteğe Bağlı)")]
    public string? PhoneNumber { get; set; }

    [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
    [StringLength(256, ErrorMessage = "E-posta adresi en fazla 256 karakter olabilir.")]
    [Display(Name = "E-posta Adresi (İsteğe Bağlı)")]
    public string? Email { get; set; }

    [StringLength(2000, ErrorMessage = "Özel notlar en fazla 2000 karakter olabilir.")]
    [Display(Name = "Öğrenci Hakkında Özel Notlar (İsteğe Bağlı)")]
    public string? Notes { get; set; }
}
