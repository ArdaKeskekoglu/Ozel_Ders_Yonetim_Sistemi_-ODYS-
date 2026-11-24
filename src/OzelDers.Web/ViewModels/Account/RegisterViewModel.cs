using System.ComponentModel.DataAnnotations;

namespace OzelDers.Web.ViewModels.Account;

/// <summary>
/// Öğretmen kayıt formu ViewModel sınıfı (FR-001, FR-022, BR-16, NFR-009).
/// Öğretmen kayıt olurken en az bir ders türü seçmek veya tanımlamak zorundadır.
/// İlk seçilen/tanımlanan ders otomatik olarak varsayılan ders (IsDefault = true) olur.
/// </summary>
public class RegisterViewModel : IValidatableObject
{
    public static readonly IReadOnlyList<string> SuggestedSubjectNames = new[]
    {
        "Matematik",
        "Geometri",
        "Fizik",
        "Kimya",
        "Biyoloji",
        "Türkçe",
        "İngilizce",
        "Tarih"
    };

    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad en az 2, en fazla 100 karakter olmalıdır.")]
    [Display(Name = "Ad")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad en az 2, en fazla 100 karakter olmalıdır.")]
    [Display(Name = "Soyad")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi zorunludur.")]
    [EmailAddress(ErrorMessage = "Lütfen geçerli bir e-posta adresi giriniz.")]
    [StringLength(256, ErrorMessage = "E-posta adresi en fazla 256 karakter olabilir.")]
    [Display(Name = "E-posta Adresi")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Lütfen geçerli bir telefon numarası giriniz.")]
    [StringLength(30, ErrorMessage = "Telefon numarası en fazla 30 karakter olabilir.")]
    [Display(Name = "Telefon Numarası (İsteğe Bağlı)")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Verdiğiniz Ders Türleri")]
    public List<string> SelectedSubjects { get; set; } = new();

    [StringLength(500, ErrorMessage = "Özel ders türü alanı en fazla 500 karakter olabilir.")]
    [Display(Name = "Diğer / Özel Ders Türleri (Virgülle Ayırarak Yazabilirsiniz)")]
    public string? CustomSubjects { get; set; }

    [Required(ErrorMessage = "Şifre alanı zorunludur.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Şifre en az 8 karakter uzunluğunda olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Şifre")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre tekrar alanı zorunludur.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Şifre ile şifre tekrarı birbiriyle eşleşmiyor.")]
    [Display(Name = "Şifre Tekrar")]
    public string ConfirmPassword { get; set; } = string.Empty;

    /// <summary>
    /// Seçilen ve özel girilen ders türlerini giriş sırasını koruyarak,
    /// boşlukları temizlenmiş ve tekrarsız şekilde döndürür. İlk eleman varsayılan derstir.
    /// </summary>
    public IReadOnlyList<string> GetNormalizedSubjectNames()
    {
        var result = new List<string>();

        if (SelectedSubjects is not null)
        {
            foreach (var item in SelectedSubjects)
            {
                AddNormalizedSubject(result, item);
            }
        }

        if (!string.IsNullOrWhiteSpace(CustomSubjects))
        {
            var customParts = CustomSubjects.Split(
                new[] { ',', ';', '\n', '\r' },
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in customParts)
            {
                AddNormalizedSubject(result, part);
            }
        }

        return result;
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var subjects = GetNormalizedSubjectNames();
        if (subjects.Count == 0)
        {
            yield return new ValidationResult(
                "Kayıt olabilmek için en az bir ders türü seçmeli veya tanımlamalısınız.",
                new[] { nameof(SelectedSubjects), nameof(CustomSubjects) });
            yield break;
        }

        if (subjects.Any(s => s.Length < 2 || s.Length > 100))
        {
            yield return new ValidationResult(
                "Her bir ders türü adı en az 2, en fazla 100 karakter olmalıdır.",
                new[] { nameof(CustomSubjects) });
        }
    }

    private static void AddNormalizedSubject(List<string> target, string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
        {
            return;
        }

        var trimmed = rawName.Trim();
        if (trimmed.Length == 0)
        {
            return;
        }

        if (!target.Any(existing => string.Equals(existing, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            target.Add(trimmed);
        }
    }
}
