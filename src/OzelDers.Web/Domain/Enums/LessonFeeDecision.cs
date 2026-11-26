namespace OzelDers.Web.Domain.Enums;

/// <summary>
/// Ders oturumu için ücretlendirme kararı (FR-065..FR-068, BR-02, BR-03).
/// Kesin İş Kuralı (BR-02, FR-065, FR-066):
/// - Öğrenci derse geldiğinde (Attended) ücret kararı otomatik olarak Charge olur.
/// - Öğrenci derse gelmediğinde (Absent) otomatik borç oluşturulmaz; öğretmen o ders için
///   ücret alınıp alınmayacağını (Charge veya Waive) ders bazında ayrı belirler.
/// - Ücret kararını henüz tamamlamayan devamsız ders finansal bakımdan Pending (bekleyen işlem) kalır (BR-03).
/// </summary>
public enum LessonFeeDecision
{
    /// <summary>
    /// Henüz ücret kararı verilmedi / işlem bekliyor (BR-02, BR-03).
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Ücret alınacak (Dersin SessionFeeSnapshot tutarı tahakkuk ettirilecek) (FR-065, FR-067).
    /// </summary>
    Charge = 1,

    /// <summary>
    /// Ücret alınmayacak / muaf tutulacak (0 TL) (FR-065, FR-066).
    /// </summary>
    Waive = 2
}
