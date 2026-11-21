# Özel Ders Yönetim Sistemi (ÖDYS)

> Özel ders öğretmenleri için öğrenci, ders türü, haftalık program, yoklama, ücret ve tahsilat yönetimi ile **tek haftalık otomatik ders programlama** uygulaması.

**Durum:** Planlama ve dokümantasyon (uygulama kodu henüz bu pakette yok).  
**Dokümantasyon sürümü:** 1.1 — 22 Kasım 2026  
**Geliştirme yaklaşımı:** Antigravity ile 45 küçük faz, her faz sonunda öğrenme, test ve kullanıcı onayı.

## Proje hedefi

Öğretmenin öğrencilerini, derslerini, ödemelerini ve bekleyen işlemlerini tek yerde yönetmesini sağlamak. Müsaitliklerden uygun haftalık plan üreten algoritma, öğretmenin onayından önce gerçek dersleri değiştirmez.

## Netleşmiş ürün kararları

1. Öğrenci derse gelmezse **ücret alınacak mı** sorusuna öğretmen **her ders için ayrı** cevap verir.
2. Otomatik planlama **yalnızca seçilen tek hafta** için yapılır.
3. Mevcut dersleri öğretmen **sabit** veya **değiştirilebilir** olarak seçer.
4. İlk sürümde sadece **öğretmen** hesabı vardır; öğrenci paneli yoktur.
5. Öğretmen birden fazla ders türü verir; öğrenci/ders türü bazlı ücret tanımlanabilir.

## Modüller

- Öğretmen kaydı, giriş, profil, güvenli veri erişimi
- Öğrenci listesi, ekleme, güncelleme, arşivleme ve detay
- Ders türleri, ders başına ücret, haftalık ihtiyaçlar
- Manuel haftalık takvim, çakışma kontrolü, tekrarlayan dersler
- Yoklama, ders tamamlanması, devamsızlık ücret kararı
- Tahakkuk, tam/kısmi/toplu tahsilat ve öğrenci borçları
- Öğrenci/öğretmen müsaitlikleri ve tek haftalık algoritma
- Taslak plan, düzenleme, alternatif öneri, onay ve transaction
- Ders geçmişi, dashboard, raporlar, uygulama içi hatırlatmalar

## Planlanan teknoloji (henüz uygulamaya geçirilmedi)

ASP.NET Core MVC / C#, Entity Framework Core, SQL Server, ASP.NET Core Identity, HTML/CSS/JavaScript, responsive tasarım. Takvim için FullCalendar, raporlar için Chart.js ve gerektiğinde OR-Tools değerlendirilebilir. Gerçek sürüm/paket seçimi geliştirme sırasında doğrulanacaktır.

## Dokümantasyon

| Belge | Konu |
|---|---|
| [01 — Ürün kapsamı](docs/01-urun-kapsami.md) | Hedef kullanıcı, kararlar, kapsam |
| [02 — 143 fonksiyonel gereksinim](docs/02-fonksiyonel-gereksinimler.md) | FR-001–FR-143 |
| [03 — İş kuralları](docs/03-is-kurallari.md) | Ders, yoklama, ücret ve ödeme |
| [04 — Sayfalar ve akışlar](docs/04-ekranlar-ve-akislar.md) | UI ve kullanıcı akışları |
| [05 — Mimari/veri modeli](docs/05-mimari-veri-modeli.md) | Veritabanı tabloları ve katmanlar |
| [06 — Otomatik programlama](docs/06-otomatik-programlama.md) | Kısıtlar, taslak, onay, atomiklik |
| [07 — API taslağı](docs/07-api-sozlesmesi.md) | Örnek uç noktalar ve sözleşmeler |
| [08 — Kalite/test/güvenlik](docs/08-kalite-test-guvenlik.md) | Kabul ve riskler |
| [09 — 45 faz yol haritası](docs/09-yol-haritasi.md) | Faz 1–45, kabul kanıtları |
| [10 — Antigravity rehberi](docs/10-antigravity-rehberi.md) | AI ile kontrollü çalışma |
| [11 — Açık kararlar](docs/11-acik-kararlar.md) | Henüz kesinleşmeyen ayrıntılar |
| [12 — Faz eşleştirmesi](docs/12-gereksinim-faz-eslestirmesi.md) | FR grubu → faz |
| [13 — 15 fonksiyonel olmayan gereksinim](docs/13-fonksiyonel-olmayan-gereksinimler.md) | NFR-001–015 |

## Geliştirme kilometre taşları

| Faz | Hedef |
|---|---|
| 1–5 | Proje altyapısı |
| 6–9 | Öğretmen hesapları |
| 10–16 | Öğrenci ve ders türü yönetimi |
| 17–23 | Manuel haftalık ders programı |
| 24–31 | Yoklama, ücret, borç ve ödeme |
| 32–39 | Otomatik tek haftalık planlama ve onay |
| 40–42 | Dashboard ve raporlar |
| 43–44 | Bildirimler ve ayarlar |
| 45 | Entegrasyon testleri ve yayın hazırlığı |

## GitHub'a yükleme

1. ZIP içeriğini çıkar. **Dış klasörü değil, içindeki dosya/klasörleri** repository köküne yerleştir.
2. `README.md`, `docs/`, `.github/`, `.gitignore` ve `.env.example` aynı kök düzeyinde olmalıdır.
3. ZIP dosyasını tek parça olarak repoya yükleme. `README.md` otomatik görüntülenir; diğer belgeler `docs/` içinde açılır.
4. Git üzerinden yüklemek gizli (`.` ile başlayan) dosyaları web arayüzünde sürükle-bırak yapmaktan daha güvenilirdir.
5. Antigravity'de önce [45 faz planını](docs/09-yol-haritasi.md) okut; Faz 1 dışında kod geliştirme onayı verme.

## Güvenlik

`.env.example` yalnızca **örnek** değerler içerebilir. `.env`, API anahtarı, gerçek bağlantı şifresi, kişisel öğrenci bilgileri veya canlı veritabanı dışa aktarımları GitHub'a gönderilmez. Private repository de sır saklama önlemlerinin yerine geçmez.

## Kapsam dışı (ilk sürüm)

Ayrı Android/iOS uygulaması, öğrenci girişi, WhatsApp otomasyonu, yapay zekâ sohbet asistanı ve çok öğretmenli kurum hesabı.
