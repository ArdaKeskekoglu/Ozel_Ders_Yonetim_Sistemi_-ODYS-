using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Account;

/// <summary>
/// Şifremi unuttum talep formu ViewModel sınıfı (FR-005).
/// </summary>
public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "Kayıtlı E-posta Adresi")]
    public string Email { get; set; } = string.Empty;
}
