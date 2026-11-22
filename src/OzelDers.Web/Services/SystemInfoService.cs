using OzelDers.Web.Services.Abstractions;

namespace OzelDers.Web.Services;

/// <summary>
/// ISystemInfoService arayüzünün varsayılan uygulaması.
/// </summary>
public class SystemInfoService : ISystemInfoService
{
    public string GetApplicationName() => "Özel Ders Yönetim Sistemi";

    public string GetShortCode() => "ÖDYS";

    public string GetDescription() =>
        "Özel ders öğretmenleri için öğrenci, ders türü, haftalık program, yoklama, ücret/tahsilat yönetimi ve tek haftalık otomatik ders programlama platformu.";
}
