namespace OzelDers.Web.Services.Abstractions;

/// <summary>
/// Uygulama temel bilgilerini ve durumunu sağlayan servis sözleşmesi.
/// </summary>
public interface ISystemInfoService
{
    string GetApplicationName();
    string GetShortCode();
    string GetDescription();
}
