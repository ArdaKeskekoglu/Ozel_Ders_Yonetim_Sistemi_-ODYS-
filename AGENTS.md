# AGENTS.md — Özel Ders Yönetim Sistemi (ÖDYS) Çalışma Talimatları

Bu dosya, yapay zekâ kod asistanlarının (Antigravity vb.) proje geliştirme sürecinde her oturumda uyması gereken zorunlu kuralları tanımlar.

## 1. Proje Özeti ve Teknoloji Yığını

- **Proje:** Özel Ders Yönetim Sistemi (ÖDYS)
- **Teknolojiler:** C#, ASP.NET Core MVC, Entity Framework Core, SQL Server, ASP.NET Core Identity
- **Geliştirme Modeli:** `docs/09-yol-haritasi.md` belgesinde tanımlanan **45 küçük faz**.
- **Temel Referanslar:** `README.md` ve `docs/` klasöründeki (`01`–`13`) tüm dokümanlar bağlayıcıdır.

### Güncel durum ve arayüz sürekliliği

- Faz 1–23 tamamlandı; Faz 24'e başlanmadı. 45 fazlık yol haritası geçerlidir. UI yenilemesinin Aşama 1–3'ü `main` dalına alındı; Aşama 4–6 henüz yapılmadı.
- Yeni veya yenilenen ekranlarda [`design-system.css`](src/OzelDers.Web/wwwroot/css/design-system.css) içindeki renk/değişkenleri ve ortak kart, tablo, form, buton, durum etiketi ve yerleşim stillerini kullan. Ortak [`_Layout.cshtml`](src/OzelDers.Web/Views/Shared/_Layout.cshtml) düzeniyle uyumlu ol; gereksiz açıklamaları çoğaltma, gerekli form etiketlerini ve hata mesajlarını koru.
- Gerçekten açıklama gereken yerde [`_InfoPopover.cshtml`](src/OzelDers.Web/Views/Shared/_InfoPopover.cshtml) bileşenini kullan; açıklama varsayılan olarak kapalı ve klavye/dokunma ile erişilebilir olmalı.
- Sol menüde Ders Türleri bağlantısı bulunmaz; yönetim Profil üzerinden erişilir. Ana Sayfa, Öğrenciler ve Ders Programı aktiftir; Ders Geçmişi, Ödemeler ve Raporlar henüz “Yakında” durumundadır. Masaüstü ve mobil menü aynı modül durumlarını göstermelidir. Ayrıntılar için [`docs/04-ekranlar-ve-akislar.md`](docs/04-ekranlar-ve-akislar.md) belgesine bak.

## 2. Faz Bazlı Geliştirme Kuralları (45 Faz)

Her fazda aşağıdaki adımlar sırasıyla ve eksiksiz uygulanır:

1. **Tek Faz Odaklılık:** Aynı anda yalnızca bir faz üzerinde çalışılır.
2. **Faz Geçiş Onayı:** Bir sonraki faza geçmeden önce mutlaka kullanıcıdan onay alınır.
3. **Ön Açıklama:** Kod yazmadan önce ilgili fazda neler yapılacağı, hangi gereksinimlerin (FR/NFR) karşılanacağı ve hangi dosyaların etkileneceği açıklanır.
4. **Değişiklik Listesi:** Faz tamamlandığında eklenen veya değiştirilen tüm dosyalar listelenir.
5. **Öğretici Açıklama:** Yazılan önemli kodların, mimari kararların ve kullanılan teknolojilerin ne işe yaradığı öğretici biçimde açıklanır.
6. **Test ve Doğrulama:** İlgili birim/entegrasyon testleri gerçekten çalıştırılır ve sonuçları kullanıcıya bildirilir. Test çalıştırılmadan başarılı olduğu iddia edilmez.
7. **Git Durum Kontrolü:** Her faz sonunda `git status` ile çalışma dizini kontrol edilir.
8. **İzole Commit:** Yalnızca o faza ait değişiklikler commit kapsamına alınır.
9. **Commit ve Push Onayı:** Kullanıcı açıkça onay vermeden `git commit` veya `git push` yapılmaz.
10. **Uzak Repo Doğrulaması:** Onay sonrası açıklayıcı bir commit mesajı ile GitHub'a push edilir ve uzak repository'nin güncellendiği doğrulanır.
11. **Hata Yönetimi:** Push veya derleme/test başarısız olursa sorun açıklanır; gerekli düzeltmeler için kullanıcıdan onay alınır.

## 3. Güvenlik ve Dosya Yönetimi Kuralları

- **Gizli Bilgiler:** Gerçek şifreler, bağlantı dizgeleri, erişim token'ları, API anahtarları veya gerçek öğrenci verileri asla GitHub'a gönderilmez.
- **Örnek Yapılandırma:** `.env.example` dosyasında yalnızca örnek değerler kullanılır.
- **Dosya Koruma:** Mevcut dokümantasyon ve proje dosyaları izinsiz silinmez veya değiştirilmez.
- **Git Güvenliği:** `git push --force` kullanılmaz ve Git geçmişi izinsiz değiştirilmez.
- **Veritabanı Güvenliği:** Veritabanı veya mevcut veriler izinsiz silinmez; yıkıcı migration işlemlerinden önce onay alınır.
- **Mimari Kararlar:** Önemli mimari ve teknoloji değişiklikleri uygulanmadan önce kullanıcıya açıklanır, açık konular için `docs/11-acik-kararlar.md` referans alınır.
- **Ders Türü ve Ders Oturumu Kuralları:**
  - Öğretmen kayıt olurken en az bir ders türü tanımlamak zorundadır ve ilk ders otomatik olarak varsayılan ders (`IsDefault = true`) olur.
  - Bir öğrenciye birden fazla ders türü (`StudentSubjects`) atanabilir ve her ders türü için saatlik ücret (`HourlyRate`), oturum süresi (`DefaultDurationMinutes`) ve haftalık oturum sayısı (`WeeklyTargetCount`) ayrı tutulur.
  - Ancak her ders oturumu (`Lessons`) **yalnızca tek bir ders türüne** (`StudentSubjectId` üzerinden) ait olabilir; tek bir ders oturumuna birden fazla ders türü atanamaz.
