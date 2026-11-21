# 09 — Yol haritası: 45 küçük faz

> Bu dosya, önceki 0–6 fazlık planın yerine geçen **güncel ve geçerli** plandır. Fazlar 1–45 numaralandırılmıştır. Kapsam büyüdüğünde alt görev açılır; otomatik geçiş yapılmaz.

## Faz tamamlama standardı

Her faz için: (1) amaç ve gereksinim ID’leri, (2) dar kapsamlı uygulama, (3) otomatik ve manuel test, (4) kod açıklaması, (5) kullanıcı incelemesi, (6) açık onaydan sonra commit/push. İşlenen kodun derlenmesi ve testlerin raporlanması zorunludur.

## Altyapı — Faz 1–5

| Faz | İş | Tamamlanma kanıtı |
|---|---|---|
| **1 — Dokümantasyon ve depo hazırlığı** | README, docs, .gitignore ve .env.example; kodsuz başlangıç | Repo kökünde dosyalar, doğru bağlantılar, gizli anahtar yok |
| **2 — MVC uygulaması oluşturma** | ASP.NET Core MVC çözümü ve ilk çalışır sayfa | Yerel web sayfası açılır; proje derlenir |
| **3 — Mimari klasör yapısı** | Controller, View, ViewModel, Services ve Data sorumlulukları | Yapı derlenir; örnek servis enjekte edilir |
| **4 — Veritabanı ve EF Core** | SQL Server konfigürasyonu, DbContext, migration | İlk migration test veritabanında çalışır |
| **5 — Ortak arayüz ve mobil uyum** | Layout, navigasyon, temel responsive stiller | Mobil ve masaüstü menüler çalışır |

## Hesap — Faz 6–9

| Faz | İş | Tamamlanma kanıtı |
|---|---|---|
| **6 — Öğretmen hesabı ve Identity** | Identity altyapısı, hesap modeli | Kimlik tabloları oluşur, şifreler güvenli saklanır |
| **7 — Kayıt olma** | Kayıt formu, doğrulama ve güvenli hata mesajı | Yeni öğretmen kaydedilir |
| **8 — Giriş, çıkış ve yetkilendirme** | Login/logout, authorize, sahiplik filtresi | Diğer öğretmenin verisine erişim reddedilir |
| **9 — Profil ve hesap bakımı** | Profil düzenleme, şifre değiştirme/sıfırlama akışları | Yetkili kullanıcı günceller; güvenli sıfırlama sınanır |

## Öğrenci ve ders türü — Faz 10–16

| Faz | İş | Tamamlanma kanıtı |
|---|---|---|
| **10 — Öğrenci veri modeli** | Öğrenci, öğretmen sahipliği, arşiv durumu | Migration + sahiplik testleri geçer |
| **11 — Öğrenci listesi** | Listeleme ve boş durum görünümü | Yalnızca kendi öğrencileri görünür |
| **12 — Öğrenci ekleme** | Form, doğrulama, kayıt | Yeni öğrenci doğru verilerle kaydolur |
| **13 — Öğrenci güncelleme/arşiv** | Düzenleme, arşiv, yeniden aktif etme | Geçmiş bağlantılar bozulmaz |
| **14 — Ders türü yönetimi** | Ders türlerini ekle, düzenle, arşivle | Ders türleri öğretmene özeldir |
| **15 — Öğrenci-ders türü ve ücret** | Çoklu ders türü, ders başına ücret, haftalık hedef | Öğrenciye farklı ücretler atanır |
| **16 — Öğrenci detayları** | Detay, notlar, filtre/arama | İlişkili bilgiler doğru gösterilir |

## Manuel program — Faz 17–23

| Faz | İş | Tamamlanma kanıtı |
|---|---|---|
| **17 — Ders kayıt modeli** | Lesson, ücret snapshot, durumlar | Ders-öğrenci-tür bağı kurulur |
| **18 — Haftalık takvim** | Hafta navigasyonu ve takvim verileri | Önceki/sonraki hafta doğru |
| **19 — Manuel ders ekleme** | Takvimden yeni ders kaydı | Tarih/saat/öğrenci/tür doğrulanır |
| **20 — Ders düzenleme** | Gün ve saat değişikliği, alan güncelleme | Değişiklik doğru ders üzerinde gerçekleşir |
| **21 — İptal ve erteleme** | İptal/erteleme durumları ve geçmişi | İptal edilen ders takvimde ayrışır |
| **22 — Çakışma ve eşzamanlılık** | Çakışma kontrolleri, atomik kayıt koruması | Eşzamanlı çakışan ders oluşturulamaz |
| **23 — Ders detayı ve tekrarlama** | Detay, haftalık tekrar kuralı, tek hafta istisnası | Tekrarlayan dersler güvenli üretilir |

## Yoklama ve ödeme — Faz 24–31

| Faz | İş | Tamamlanma kanıtı |
|---|---|---|
| **24 — Yoklama ve ders gerçekleşme durumu** | Geldi/gelmedi/iptal/ertelendi ve bekleyen doğrulama | Saat geçti diye otomatik tamamlanmaz |
| **25 — Devamsızlıkta ücret kararı** | Gelmediyse öğretmen o ders için seçer | Otomatik borç oluşmaz |
| **26 — Ders ücret tahakkuku** | Ders ücreti snapshot, ücret kaydı ve tekrar koruması | Tek ders için çift tahakkuk yok |
| **27 — Tam ödeme kaydı** | Tahsilat oluşturma ve doğrulama | Ödeme gelir; borç düşer |
| **28 — Kısmi ve toplu tahsilat** | Payment allocations ve çoklu ders dağılımı | Kısmi/toplu tutarlar tutarlıdır |
| **29 — Bakiye hesapları** | Öğrenci borcu ve toplam alacak | Tahakkuk - tahsilat eşittir kalan |
| **30 — Ödeme geçmişi ve düzeltme** | Filtreleme, kontrollü düzeltme, denetim izi | Değişiklikler izlenir |
| **31 — Ders geçmişi ve eksik işlemler** | Geçmiş, filtreler, yoklama bekleyen işlemler | Unutulan ders tamamlanabilir |

## Otomatik program — Faz 32–39

| Faz | İş | Tamamlanma kanıtı |
|---|---|---|
| **32 — Öğretmen müsaitlikleri** | Çalışma gün ve saatleri, mola, istisnalar | Geçerli zaman aralıkları saklanır |
| **33 — Öğrenci müsaitlikleri** | Düzenli ve haftaya özel uygunluklar | Algoritma için veriler hazırlanır |
| **34 — Algoritmanın kuralları ve testleri** | Zorunlu kısıtlar, hedef dersler, çakışma/mola testleri | Geçersiz planlar reddedilir |
| **35 — Temel tek haftalık algoritma** | Uygun yerleşim ve yerleştirilemeyen ders açıklaması | Tek hafta, çakışmasız öneri üretir |
| **36 — Sabit ve değiştirilebilir dersler** | Seçili mevcut dersleri koruma veya taşıma | Sabit ders yerinde kalır |
| **37 — Program taslağı önizlemesi** | Taslak kaydı ve haftalık görünüm | Canlı ders programı etkilenmez |
| **38 — Taslak düzenleme ve alternatifler** | Taslak üzerinde düzenleme, yeniden öneri | Değişiklikler kontrol edilir |
| **39 — Program onayı ve atomik kayıt** | Onay yeniden doğrulama, transaction, idempotency | Tek onay tek sonuç; hata rollback |

## Dashboard ve raporlar — Faz 40–42

| Faz | İş | Tamamlanma kanıtı |
|---|---|---|
| **40 — Dashboard özetleri** | Aktif öğrenci, haftalık ders, tahsilat, alacak | Kartlar gerçek veriyi doğru yansıtır |
| **41 — Bugünkü dersler ve bekleyen işlemler** | Yaklaşan ders, hızlı ekleme, bekleyen kayıt | Eylemler doğru sayfaya yönlenir |
| **42 — Raporlama ve grafikler** | Ders, saat, iptal, ödeme ve bakiye raporları | Rapor toplamları kayıtlarla tutarlıdır |

## Bildirimler ve ayarlar — Faz 43–44

| Faz | İş | Tamamlanma kanıtı |
|---|---|---|
| **43 — Uygulama içi hatırlatmalar** | Yaklaşan ders/bekleyen yoklama; yinelenen uyarı önleme | Bildirimler tercihlere uyar |
| **44 — Ayarlar ve kullanıcı tercihleri** | Ders süresi, mola, çalışma saatleri, bildirimler | Değişiklikler uygulanır, doğrulanır |

## Test ve yayın — Faz 45

| Faz | İş | Tamamlanma kanıtı |
|---|---|---|
| **45 — Uçtan uca test ve yayına hazırlık** | Regresyon, güvenlik, mobil, yedek/geri yükleme, dağıtım | Kritik senaryolar geçer; üretim riskleri belgelenir |

## Kontrol noktaları

- **Faz 9:** öğretmen hesabıyla güvenli giriş.
- **Faz 16:** öğrenci ve ders türü yönetimi.
- **Faz 23:** manuel haftalık program ve tekrarlar.
- **Faz 31:** yoklama, alacak, ödeme ve geçmiş.
- **Faz 35:** ilk otomatik haftalık program önerisi.
- **Faz 39:** taslak onayıyla gerçek programa güvenli yazım.
- **Faz 45:** bütünleşik ve test edilmiş ilk kapsamlı sürüm.

## Değişiklik ve onay kuralları

- Antigravity kendiliğinden bir sonraki faza geçmez.
- Commit, push, migration uygulama, veritabanı silme ve önemli paket yüklemeleri onay gerektirir.
- Sırlar veya gerçek öğrenci verileri repoya eklenmez.
- Bir sonraki faza geçmeden önce güncel durum ve test sonucu not edilir.
- Faz 23 tekrarlayan dersleri, Faz 9 şifre sıfırlamayı, Faz 30 ödeme düzeltmeyi içerir; bu işler faz içinde alt görevlere ayrılabilir.
