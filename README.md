# Özel Ders Yönetim Sistemi (ÖDYS)

> Özel ders öğretmenleri için öğrenci, ders türü, haftalık program, yoklama, ücret ve tahsilat yönetimi ile **tek haftalık otomatik ders programlama** uygulaması.

**Durum:** Faz 1–23 tamamlandı; Faz 24'e başlanmadı. UI yenilemesinin Aşama 1–3 değişiklikleri `main` dalında (`cd22c9a`, `f8afc52`); Aşama 4–6 henüz yapılmadı.

**Geliştirme yaklaşımı:** Antigravity ile 45 küçük faz, her faz sonunda öğrenme, test ve kullanıcı onayı.

## Proje hedefi

Öğretmenin öğrencilerini, derslerini, ödemelerini ve bekleyen işlemlerini tek yerde yönetmesini sağlamak. Müsaitliklerden uygun haftalık plan üreten algoritma, öğretmenin onayından önce gerçek dersleri değiştirmez.

## Netleşmiş ürün kararları

1. Öğrenci derse gelmezse **ücret alınacak mı** sorusuna öğretmen **her ders için ayrı** cevap verir.
2. Otomatik planlama **yalnızca seçilen tek hafta** için yapılır.
3. Mevcut dersleri öğretmen **sabit** veya **değiştirilebilir** olarak seçer.
4. İlk sürümde sadece **öğretmen** hesabı vardır; öğrenci paneli yoktur.
5. Öğretmen kayıt olurken **en az bir ders türü** seçmek veya tanımlamak zorundadır; ilk ders otomatik olarak öğretmenin **varsayılan dersi** olur. Öğretmen sonradan yeni ders türleri ekleyebilir ve varsayılan dersini değiştirebilir.
6. Bir öğrenciye farklı saatlik ücret, ders süresi ve haftalık oturum hedefleriyle **birden fazla ders türü** atanabilir (`StudentSubjects`); ancak takvimdeki **her bir ders oturumu (`Lessons`) yalnızca tek bir ders türüne** ait olabilir.

## Ürün kapsamındaki modüller

- Öğretmen kaydı, giriş, profil, güvenli veri erişimi
- Öğrenci listesi, ekleme, güncelleme, arşivleme ve detay
- Ders türleri, ders başına ücret, haftalık ihtiyaçlar
- Manuel haftalık takvim, çakışma kontrolü, tekrarlayan dersler
- Yoklama, ders tamamlanması, devamsızlık ücret kararı
- Tahakkuk, tam/kısmi/toplu tahsilat ve öğrenci borçları
- Öğrenci/öğretmen müsaitlikleri ve tek haftalık algoritma
- Taslak plan, düzenleme, alternatif öneri, onay ve transaction
- Ders geçmişi, dashboard, raporlar, uygulama içi hatırlatmalar

## Teknoloji

Uygulama ASP.NET Core MVC / C#, Entity Framework Core, SQL Server, ASP.NET Core Identity ve HTML/CSS/JavaScript kullanır. Responsive arayüzün ortak stilleri [`design-system.css`](src/OzelDers.Web/wwwroot/css/design-system.css) dosyasındadır. İlerideki modüller için FullCalendar, Chart.js ve gerektiğinde OR-Tools değerlendirilebilir; bunların kullanımı mevcut uygulamada varsayılmamalıdır.

## Mevcut arayüz temeli

UI Aşama 1–3 ile ortak renk, tipografi, kart, tablo, form, buton ve durum etiketleri; responsive yerleşim ve gerektiğinde açılan erişilebilir bilgi ikonları eklendi. Sol menüde Ana Sayfa, Öğrenciler ve Ders Programı çalışır. Ders Geçmişi, Ödemeler ve Raporlar “Yakında” etiketiyle pasiftir. Ders Türleri sol menüde yoktur; yönetimi Profil üzerinden erişilebilir. Öğrenci listesi masaüstünde tablo, dar ekranda kart düzeni kullanır; arama, filtre ve mevcut öğrenci işlemleri korunur. Ayrıntılar için [ekranlar ve akışlar](docs/04-ekranlar-ve-akislar.md) belgesine bakın.

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

## Geliştirmeye devam

Faz 24 ve sonraki işler için [45 faz yol haritasını](docs/09-yol-haritasi.md) ve [çalışma talimatlarını](AGENTS.md) izleyin. Yeni ekranları mevcut [`design-system.css`](src/OzelDers.Web/wwwroot/css/design-system.css) ve ortak Razor bileşenleriyle uyumlu geliştirin; henüz yapılmamış UI Aşama 4–6'yı tamamlanmış kabul etmeyin.

## Güvenlik

`.env.example` yalnızca **örnek** değerler içerebilir. `.env`, API anahtarı, gerçek bağlantı şifresi, kişisel öğrenci bilgileri veya canlı veritabanı dışa aktarımları GitHub'a gönderilmez. Private repository de sır saklama önlemlerinin yerine geçmez.

## Kapsam dışı (ilk sürüm)

Ayrı Android/iOS uygulaması, öğrenci girişi, WhatsApp otomasyonu, yapay zekâ sohbet asistanı ve çok öğretmenli kurum hesabı.
