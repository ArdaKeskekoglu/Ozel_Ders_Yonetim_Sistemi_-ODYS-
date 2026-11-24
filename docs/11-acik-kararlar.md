# 11 — Açık kararlar ve varsayımlar

Aşağıdaki maddeler **henüz kullanıcı tarafından kesinleştirilmiş karar değildir**. Kodlanmadan önce netleştirilmelidir.

| ID | Açık konu | Başlangıç önerisi |
|---|---|---|
| DEC-01 | Öğrenci gelmesine rağmen ders erken biterse ücret nasıl hesaplanacak? | Öğretmenin düzenleyebileceği ders ücreti snapshot + gerekçe |
| DEC-02 | Ders ücretine ilk sürümde indirim/ek ücret uygulanacak mı? | Şimdilik manuel tek ders ücreti düzeltmesi; denetim izi |
| DEC-03 | Toplu ödeme açık borçlara nasıl dağıtılacak? | Öğretmen açık tahakkukları seçsin; otomatik dağıtım opsiyonel |
| DEC-04 | Ön ödeme ve iade desteklenecek mi? | İlk sürüm kapsam dışı; hata yerine kontrollü kural |
| DEC-05 | Otomatik algoritma tüm dersleri yerleştiremezse kısmi öneri onaylanabilir mi? | Eksikleri göster; açık onay iste |
| DEC-06 | Saat aralığı slot hassasiyeti ne olacak? | Başlangıçta 30 dakika; ayarlanabilirlik sonraki faz |
| DEC-07 | Tekrarlayan ders kuralının geleceğe uygulanma sınırı ne olacak? | İleriye dönük belirli ufuk; geçmiş dersleri değiştirmeme |
| DEC-08 | Müsaitlik dışına manuel ders eklenmesine izin verilecek mi? | Uyarı/özel yetki, çakışmaya yine izin yok |
| DEC-09 | Grup dersi desteği olacak mı? | İlk sürüm kapsam dışı |
| DEC-10 | Öğretmenler için para birimi ve saat dilimi seçenekleri? | İlk kullanım TRY; öğretmen saat dilimi saklanır |
| DEC-11 | Bildirim kanalı ve zamanlaması ne olacak? | Önce uygulama içi bekleyen işler, harici bildirim sonra |
| DEC-12 | Ücret ve ders değişikliklerinde denetim kaydının ayrıntı düzeyi? | Yeterli olay kaydı; gereksiz kişisel bilgi yok |
| DEC-13 | Öğrenci iletişim bilgileri zorunlu mu? | Ad-soyad zorunlu, telefon/e-posta isteğe bağlı |
| DEC-14 | Ders türlerinin standart listesi mi, özel isim mi? | **Kesinleşti (2026-11-24):** Öğretmen kayıt olurken önerilen derslerden seçebilir ve/veya serbestçe tanımlayabilir (en az 1 ders zorunlu, ilk ders varsayılan olur). |
| DEC-15 | Üretimde barındırma, yedekleme, hukuk süreçleri | Ürünleşme aşamasında ayrıca belirle |

## Değişiklik günlüğü

- **2026-10-27 — İlk dokümantasyon paketi:** Önceki görüşmelerde kesinleşen 5 ana ürün kararı işlendi; diğer konular öneri veya açık karar olarak ayrıştırıldı.
- **2026-11-24 — Ders türleri ve ders oturumu kuralları güncellemesi:** Öğretmen kaydında zorunlu ders türü ve varsayılan ders (`IsDefault`), öğrenciye çoklu ders türü atanabilmesi (`StudentSubjects`: `HourlyRate`, `DefaultDurationMinutes`, `WeeklyTargetCount`) ve her ders oturumunun (`Lessons`) yalnızca tek bir ders türüne (`StudentSubjectId`) ait olması kuralları kesinleştirildi.
