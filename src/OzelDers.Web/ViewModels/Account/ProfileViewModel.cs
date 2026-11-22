using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Account;

/// <summary>
/// Öğretmen profil düzenleme ViewModel sınıfı (FR-006, DEC-10, NFR-009).
/// </summary>
public class ProfileViewModel
{
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad en az 2, en fazla 100 karakter olmalıdır.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad en az 2, en fazla 100 karakter olmalıdır.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Görünen ad en fazla 200 karakter olabilir.")]
    [Display(Name = "Görünen Ad (İsteğe Bağlı)")]
    public string? DisplayName { get; set; }

    [Display(Name = "E-posta Adresi")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Lütfen geçerli bir telefon numarası giriniz.")]
    [StringLength(30, ErrorMessage = "Telefon numarası en fazla 30 karakter olabilir.")]
    [Display(Name = "Telefon Numarası")]
    public string? PhoneNumber { get; set; }

    [Required(ErrorMessage = "Saat dilimi seçimi zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Saat Dilimi")]
    public string TimeZoneId { get; set; } = "Turkey Standard Time";

    [Display(Name = "Para Birimi")]
    public string CurrencyCode { get; set; } = "TRY";
}
