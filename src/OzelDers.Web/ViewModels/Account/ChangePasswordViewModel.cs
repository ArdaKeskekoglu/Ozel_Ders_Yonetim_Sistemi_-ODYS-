using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Account;

/// <summary>
/// Öğretmen şifre değiştirme ViewModel sınıfı (FR-004, NFR-009).
/// </summary>
public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut şifre alanı zorunludur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Mevcut Şifre")]
    public string CurrentPassword { get; set; } = string.Empty;

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
