using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Account;

/// <summary>
/// Öğretmen giriş formu ViewModel sınıfı (FR-002, NFR-009).
/// </summary>
public class LoginViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta Adresi")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre alanı zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni Hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}
