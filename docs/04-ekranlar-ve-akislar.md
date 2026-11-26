> **Güncelleme (2026-11-22):** Genel kapsam için `02-fonksiyonel-gereksinimler.md`, geçerli geliştirme planı için `09-yol-haritasi.md` esas alınır. Bu belgedeki örnek mimari ve API kararları taslaktır.

# 04 — Ekranlar ve kullanıcı akışları

## Bilgi mimarisi

Mevcut masaüstü ve mobil menüde **Ana Sayfa · Öğrenciler · Ders Programı** bağlantıları çalışır; **Ders Geçmişi · Ödemeler · Raporlar** “Yakında” etiketiyle pasiftir ve çalışmayan sayfaya yönlendirmez. Ders Türleri sol menüde bulunmaz; Profil içindeki **Ders Türlerini Yönet** bağlantısından erişilir. Üstte Profil çalışır; Bildirimler ve Ayarlar “Yakında” durumundadır. Mobil menü aynı öğeleri açılır panelde gösterir.

### Tamamlanan UI temeli (Aşama 1–3)

[`design-system.css`](../src/OzelDers.Web/wwwroot/css/design-system.css), lacivert metin/yan menü, mavi ana işlem rengi (`#2458c7`), açık gri zemin (`#f5f7fb`), beyaz yüzey ve tutarlı kenarlık/boşluk değerlerini tanımlar. Yazı tipi önceliği Segoe UI, Inter ve sistem fontlarıdır. Ortak kart (`.odys-card`), Bootstrap uyumlu buton/form/tablo stilleri ve metinli durum etiketleri yeni ekranlarda da kullanılmalıdır. [`_Layout.cshtml`](../src/OzelDers.Web/Views/Shared/_Layout.cshtml) bu stilleri yükler; klavye için “İçeriğe geç” bağlantısı ve responsive navigasyonu sağlar.

Başlık yanında yalnızca gerekli açıklamalar için [`_InfoPopover.cshtml`](../src/OzelDers.Web/Views/Shared/_InfoPopover.cshtml) kullanılır. Bilgi simgesi açıklamayı tıklama/dokunma ile açar; kapatma düğmesi, dışarı tıklama ve Escape ile kapanır. Erişilebilir ad ve açık/kapalı durumu duyurulur. Form etiketleri, hata ve doğrulama mesajları sadeleştirme sırasında korunur.

### Sayfa haritası

```text
/Account/Register, /Account/Login, /Account/ForgotPassword
/Dashboard
/Students → /Students/Details/{id} → Edit / Availability / Subjects / Lessons / Payments
/Subjects
/Schedule → AddLesson / LessonDetails / AutoGenerate / DraftPreview
/Lessons → Details / Complete / Attendance
/Payments → New / History / Receivables
/Reports
/Settings → Profile / WorkingHours / Notifications
```

Bu harita ürün hedefidir; tüm yolların şu anda mevcut olduğunu göstermez. Mevcut ana sayfa `/`, öğrenci listesi `/Students`, profil `/Account/Profile`, ders türleri yönetimi `/Subjects` üzerinden açılır. Gelecek ekranların gerçek route adları ilgili fazda belirlenir.

## Ekran 1 — Dashboard

Üst özet kartları: aktif öğrenci, bu haftaki ders sayısı, bekleyen ders işlem sayısı, toplam alacak. Alt kısım: bugünkü dersler, yaklaşan ders, hızlı işlemler ve küçük takvim görünümü. "Bekleyen işlemi tamamla" ilgili dersin ekranına götürmeli.

## Ekran 2 — Öğrenci listesi ve detayı

Mevcut `/Students` listesinde öğrenci adı/e-posta, ders türleri, telefon, durum, not ve işlemler gösterilir. Öğrenci adı detaya gider; ders/ücret yönetimi ve düzenleme bağlantıları ile arşivleme/geri alma işlemleri korunur. Arama, ders türü ve durum filtreleri vardır. Masaüstünde okunabilir tablo, dar ekranda aynı içerikten kart görünümü kullanılır; sonuçsuz arama ve boş liste durumları ayrı gösterilir. Sonraki ders, açık bakiye ve planlanan detay sekmeleri ileriki fazların hedefidir; bugün uygulanmış sayılmamalıdır.

## Ekran 3 — Ders türleri ve kayıt/profil bağlantısı

- **Kayıt ekranı (`/Account/Register`):** Öğretmen kayıt olurken en az bir ders türü seçmek veya tanımlamak zorundadır. İlk seçilen/tanımlanan ders otomatik olarak öğretmenin **varsayılan dersi** olur.
- **Profil ekranı (`/Account/Profile`):** Öğretmenin mevcut varsayılan dersi görüntülenir ve Ders Türlerini Yönet bağlantısı `/Subjects` ekranını açar. Henüz dersi olmayan eski hesaplar için tamamlayıcı ilk ders türü tanımlama alanı sunulur.
- **Ders türleri yönetim ekranı (`/Subjects`):** Öğretmenin kendi ders türlerini eklediği, düzenlediği, varsayılan dersini değiştirdiği ve eski kayıt kullanan türleri güvenli biçimde arşivlediği liste. Tek aktif ders varsa otomatik varsayılan kalır.
- **Öğrenciye ders atama:** Öğrenci detayında ders atanırken öğretmenin varsayılan dersi otomatik seçili gelir; öğretmen isterse başka ders türünü seçebilir. Bir öğrenciye birden fazla ders türü atanabilir ve her öğrenci–ders türü ilişkisi için saatlik ücret (`HourlyRate`), oturum süresi (`DefaultDurationMinutes`) ve haftalık ders oturumu adedi (`WeeklyTargetCount`) ayrı ayarlanır.

## Ekran 4 — Haftalık takvim

Hafta seçici (önceki/bu/sonraki), günlük saat ızgarası, ders kartları, hızlı ders ekle, otomatik program oluştur. Ders kartına tıklanınca detay, düzenle, ertele, iptal, tamamla seçenekleri. Mobilde isteğe bağlı gün listesi görünümü.

### Manuel ders ekleme formu

Öğrenci, öğrenciye atanmış **tek bir ders türü (`StudentSubjectId`)**, tarih, başlangıç, süre/bitiş, ücret önizlemesi, not. **Her ders oturumu yalnızca tek bir ders türüne ait olabilir.** Kaydederken öğrencinin aktifliği, öğrenci–ders türü ilişkisi, çalışma saatleri (öğretmen izin verecekse), çakışma ve geçerlilik doğrulanır.

## Ekran 5 — Ders tamamlama

1. Öğrenci katıldı mı? `Geldi`, `Gelmedi`, `Ders iptal edildi`.
2. Gelmediyse/iptalse **bu derste ücret alınacak mı?** Evet/Hayır.
3. Ücret doğuyorsa ödeme durumu: `Tam alındı`, `Kısmi`, `Alınmadı`, `Sonra kaydet`.
4. Kaydetme öncesi özet: ücret, tahsil edilen, kalan bakiye.

Ödeme alanları ve yoklama tek ekranda sunulabilir; backend işlem modelleri ayrı tutulmalıdır.

## Ekran 6 — Ödemeler

Alacaklar (öğrenci bazında), tahsilat hareketleri, yeni tahsilat formu. Kısmi/toplu ödeme için hangi açık tahakkuklara dağıtılacağı seçilebilir; otomatik en eski borçtan dağıtma tercihi ancak öğretmenin onayıyla uygulanır.

## Ekran 7 — Otomatik program sihirbazı

1. **Hafta seçimi:** Pazartesi–pazar; yalnızca tek hafta.
2. **Öğrenci/ders türü seçimi:** Hedef ders adedi ve süreleri gösterilir.
3. **Mevcut derslerin sınıflandırılması:** Sabit veya değişebilir.
4. **Tercihler:** Mola, arka arkaya ders, boş bırakılmak istenen gün (tercih kısıtı).
5. **Taslak önizlemesi:** Eski/yeni saat, yerleşen/yerleşmeyen ders sayısı ve gerekçe.
6. **Onay:** Sunucuda yeniden doğrulama, transaction, başarı ekranı; hata varsa onay yok.

## Ekran 8 — Ders geçmişi / raporlar / ayarlar

Ders geçmişi: tarih, öğrenci, tür, katılım, ödeme durumu filtreleri. Raporlar: dönemlik ders sayısı, saat, tahakkuk, tahsilat, alacak, devamsızlık. Ayarlar: profil, çalışma aralıkları, ders ve mola varsayılanları, bildirimler.

## UX kuralları

- Mobilde bir el ile hızlı yoklama/ödeme girişi yapılabilecek geniş dokunma hedefleri.
- Silme ve finansal değişikliklerde açıklayıcı onay, geriye alma veya düzeltme akışı.
- Para, saat ve tarih formatları kullanıcı yereline uygun; sunucuda doğrulama şart.
- Renk **tek başına** durum anlatmamalı; durum etiketi de gösterilmeli.
- Form hataları alanın hemen yanında gösterilmeli.
- Taslak ve onaylı dersler açıkça ayırt edilmeli.
- Boş liste, ilk kullanım ve başarısız planlama durumları için bilgilendirici ekranlar tasarlanmalı.
