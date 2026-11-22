using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Account;

/// <summary>
/// Güvenli token ile şifre sıfırlama ViewModel sınıfı (FR-005, NFR-009).
/// </summary>
public class ResetPasswordViewModel
{
    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta Adresi")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sıfırlama anahtarı (token) zorunludur.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre alanı zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Yeni şifre en az 8 karakter uzunluğunda olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni Şifre")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre tekrar alanı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Yeni şifre ile şifre tekrarı birbiriyle eşleşmiyor.")]
    [Display(Name = "Yeni Şifre Tekrar")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
