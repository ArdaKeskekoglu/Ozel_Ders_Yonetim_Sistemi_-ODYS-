namespace OzelDers.Web.Domain.Enums;

/// <summary>
/// Ders oturumu için öğrencinin katılım / yoklama durumu (FR-057..FR-064, BR-01).
/// Kesin İş Kuralı (BR-01, FR-060, FR-061):
/// - Dersin bitiş saati (EndUtc) geçtiğinde sistem yoklamayı otomatik olarak Attended yapmaz.
/// - Öğretmen yoklama doğrulamasını yapana kadar AttendanceStatus = Pending olarak kalır.
/// </summary>
public enum LessonAttendanceStatus
{
    /// <summary>
    /// Henüz yoklama girilmedi / doğrulama bekliyor (FR-060, FR-061).
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Öğrenci derse geldi ve ders gerçekleşti (FR-057, FR-058).
    /// </summary>
    Attended = 1,

    /// <summary>
    /// Öğrenci derse gelmedi / devamsızlık yaptı (FR-058, FR-065, FR-066).
    /// Not: Öğrenci gelmediğinde otomatik borç oluşturulmaz; ücret kararı Faz 25'te öğretmen tarafından belirlenir.
    /// </summary>
    Absent = 2,

    /// <summary>
    /// Ders iptal edildiği için katılım uygulanamaz (FR-059).
    /// </summary>
    NotApplicable = 3
}
