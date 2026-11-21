> **Güncelleme (2026-11-22):** Genel kapsam için `02-fonksiyonel-gereksinimler.md`, geçerli geliştirme planı için `09-yol-haritasi.md` esas alınır. Bu belgedeki örnek mimari ve API kararları taslaktır.

# 04 — Ekranlar ve kullanıcı akışları

## Bilgi mimarisi

Masaüstünde sol menü: **Ana Sayfa · Öğrenciler · Ders Programı · Ders Geçmişi · Ödemeler · Raporlar**. Sağ üstte bildirim, profil ve ayarlar. Mobilde sık kullanılan sayfalar kolay erişilebilir olmalı; kalan menüler açılır menüde olabilir.

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

Gerçek route adları geliştirmede değişebilir.

## Ekran 1 — Dashboard

Üst özet kartları: aktif öğrenci, bu haftaki ders sayısı, bekleyen ders işlem sayısı, toplam alacak. Alt kısım: bugünkü dersler, yaklaşan ders, hızlı işlemler ve küçük takvim görünümü. "Bekleyen işlemi tamamla" ilgili dersin ekranına götürmeli.

## Ekran 2 — Öğrenci listesi ve detayı

Liste sütunları: ad-soyad, ders türleri, aktiflik, bir sonraki ders, toplam açık bakiye. Arama ve filtre. Detay sekmeleri: Genel Bilgiler, Dersler, Ödemeler, Müsaitlik, Notlar. Öğrenciyi arşivleme geri alınabilir olmalı.

## Ekran 3 — Ders türleri

Öğretmenin kendi ders türlerini eklediği/düzenlediği liste. Öğrenci detayında ilgili ders türü için saatlik ücret, varsayılan süre ve haftalık hedef adet ayarlanabilir. Eski kayıt kullanan tür arşivlenebilir.

## Ekran 4 — Haftalık takvim

Hafta seçici (önceki/bu/sonraki), günlük saat ızgarası, ders kartları, hızlı ders ekle, otomatik program oluştur. Ders kartına tıklanınca detay, düzenle, ertele, iptal, tamamla seçenekleri. Mobilde isteğe bağlı gün listesi görünümü.

### Manuel ders ekleme formu

Öğrenci, ders türü, tarih, başlangıç, süre/bitiş, ücret önizlemesi, not. Kaydederken öğrencinin aktifliği, tür ilişkisi, çalışma saatleri (öğretmen izin verecekse), çakışma ve geçerlilik doğrulanır.

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
