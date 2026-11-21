# 10 — Antigravity ile çalışma rehberi

Bu belge kod asistanını kontrollü kullanmak için görev önerileri içerir. Aracın belirli sürümüne bağlı özellikleri varsaymaz.

## İlk çalıştırma prompt'u — sadece analiz

```text
Bu repository'deki README.md ve docs/*.md dosyalarını oku.
Şimdilik hiçbir dosyayı değiştirme, kod üretme veya terminalde yıkıcı komut çalıştırma.
1) Ürünün hedefini ve kesin iş kararlarını özetle.
2) Gereksinimlerde teknik belirsizlik ve çelişki varsa ilgili belge/başlıkla belirt.
3) Önerilen ASP.NET Core MVC + EF Core mimarisini değerlendir.
4) Faz 1 için görev sırası ve test yaklaşımı hazırla.
5) Başlamadan önce çözülmesi gereken kritik soruları ayrı listele.
Yanıtı Türkçe ver; varsayımları kesin karar gibi sunma.
```

## Faz 1 uygulama prompt'u

```text
README.md, docs/05-mimari-veri-modeli.md, docs/08-kalite-test-guvenlik.md ve docs/09-yol-haritasi.md belgelerine göre yalnızca Faz 1 iskeletini kur.
C# ASP.NET Core MVC, EF Core, Identity ve test projelerini uygun katmanlarla yapılandır.
İşlevsel öğrenci/ders ekranlarını henüz yazma. Mevcut dosyaları silme.
Yaptığın değişikliklerin dosya listesini ve her komutun sonucunu raporla.
Derleme/test/migration komutlarını çalıştırabiliyorsan çalıştır ve sonucu bildir.
```

## Tek özellik geliştirme prompt şablonu

```text
Görev: [örn. STU-01, STU-02, SUB-03]
Önce ilgili dokümanları oku ve kısa plan çıkar.
Yalnızca bu gereksinimler için gerekli değişiklikleri yap.
İş mantığını controller içine yığma; doğrulama ve öğretmen sahipliği kontrolünü sunucuda uygula.
İlgili birim/entegrasyon testlerini yaz.
Migration gerekiyorsa ekle, mevcut veriyi tehlikeye atan işlemleri işaretle.
En sonda: değişen dosyalar, yapılan işler, çalıştırılan testler, kalan işler, riskler.
```

## Otomatik program özelliği prompt'u

```text
SCH-01..SCH-12 ve docs/06-otomatik-programlama.md gereksinimlerini oku.
Önce algoritma giriş/çıkış veri yapılarını ve hard/soft constraint modelini öner.
Canlı Lessons tablosuna yazmadan deterministik taslak üreten servis tasarla.
Sabit derslerin değişmemesi, haftanın dışına çıkılmaması, çakışmasızlık için testler yaz.
Onay kısmını ayrıca ele al; sürüm kontrolü, idempotency ve transaction olmadan canlı takvimi değiştirme.
Önce planı ve riskleri açıkla; sonra küçük, gözden geçirilebilir adımlarla uygula.
```

## Kod üretirken uyulacak kurallar

1. README ve ilgili `docs` belgeleri kaynak gereksinimdir; değişiklik gerekiyorsa farkı açıkla.
2. Tek prompt'ta bütün ürünü yazdırma; faz/modül bazlı ilerle.
3. Veri migrasyonlarından önce yedekleme ve etki analizi yap.
4. Kimlik, sahiplik, tarih/saat, decimal tutar ve concurrency testlerini atlama.
5. Kodun çalıştığını varsayma; gerçekten build/test çıktısı iste.
6. Görev bitince dokümanların güncelliğini kontrol et.
7. API anahtarı, bağlantı parolası veya gerçek öğrenci verisi commit etme.

## Git iş akışı önerisi

- `main`: çalışan ana dal.
- Özellik dalları: `feature/student-management`, `feature/manual-schedule`, `feature/scheduling-drafts`.
- Küçük commit ve PR; açıklamada gereksinim ID'si (örn. `STU-01`).
- Her özellikte kısa elle kullanım testi ve otomatik test sonucu ekle.


## Güncel 45 faz için kesin çalışma kuralı (2026-11-22)

> Üstteki eski büyük fazlara gönderme yapan öneriler geçersizdir. Bağlayıcı yol haritası `docs/09-yol-haritasi.md` içindeki Faz 1–45'tir.

Başlarken şu talimatı ver:

```text
Bu repository Özel Ders Yönetim Sistemi projesidir.
Önce README.md, docs/02-fonksiyonel-gereksinimler.md, docs/03-is-kurallari.md,
docs/09-yol-haritasi.md, docs/11-acik-kararlar.md,
docs/12-gereksinim-faz-eslestirmesi.md dosyalarını oku.
İlk görevin yalnızca Faz 1'i incelemek ve uygulanacak küçük adımları açıklamak.
Ben açıkça izin vermeden kodu değiştirme veya sonraki faza geçme.
Her fazda: amaç, gereksinim ID'leri, değişecek dosyalar, testler,
öğrenmem gereken kavramlar, olası riskler ve kabul kriterlerini bildir.
Testleri gerçekten çalıştırmadan başarılı olduklarını iddia etme.
Commit/push, migration, silme ve gizli bilgi işlemleri için onay iste.
Gerçek öğrenci verisi üretme ya da repoya kaydetme.
Eksik/çelişkili konularda docs/11-acik-kararlar.md'yi referans göster.
```
