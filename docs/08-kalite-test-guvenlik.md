> **Güncelleme (2026-11-22):** Genel kapsam için `02-fonksiyonel-gereksinimler.md`, geçerli geliştirme planı için `09-yol-haritasi.md` esas alınır. Bu belgedeki örnek mimari ve API kararları taslaktır.

# 08 — Kalite, test ve güvenlik gereksinimleri

## Fonksiyonel olmayan gereksinimler

| ID | Gereksinim |
|---|---|
| NFR-01 | Responsive ve erişilebilir arayüz; renk tek durum göstergesi değildir. |
| NFR-02 | Kullanıcı, kritik günlük işlemleri az adımla tamamlar. |
| NFR-03 | Kimlik doğrulama ve sunucu tarafı nesne sahipliği yetkilendirmesi zorunlu. |
| NFR-04 | Parolalar güvenli kimlik sistemiyle hash'lenir; kaynak kodda sır tutulmaz. |
| NFR-05 | HTTPS, CSRF/XSS/SQL injection önlemleri ve giriş doğrulaması sağlanır. |
| NFR-06 | Finansal işlemler ve taslak onayı atomik, izlenebilir ve eşzamanlılığa dayanıklı olur. |
| NFR-07 | Hatalar kullanıcıya anlaşılır, loglara güvenli biçimde kaydedilir. |
| NFR-08 | Yedekleme ve geri yükleme planı hazırlanır ve test edilir. |
| NFR-09 | Kişisel veri işleme amacı, saklama süresi ve silme/erişim talepleri için uygun süreç hazırlanır. |
| NFR-10 | Normal kullanımda performans hedefleri gerçekçi ölçümlerle belirlenir; gereksiz sorgular engellenir. |
| NFR-11 | Test ortamında yalnızca sentetik/sahte öğrenci verisi kullanılır. |

## Temel test senaryoları

| ID | Senaryo | Beklenen sonuç |
|---|---|---|
| T-01 | Öğretmen A, öğretmen B'nin öğrenci ID'sini açar | Yetkisiz erişim engellenir |
| T-02 | İki ders 10.00–11.00 ve 10.30–11.30 | Çakışma hatası |
| T-03 | Dersler 10.00–11.00 ve 11.00–12.00 | Mola zorunlu değilse kabul |
| T-04 | Öğrenci gelmedi, ücret alınmasın | Yeni tahakkuk oluşmaz |
| T-05 | Öğrenci gelmedi, ücret alınsın | Tahakkuk oluşur, tahsilat otomatik varsayılmaz |
| T-06 | 1.000 TL tahakkuk, 600 TL tahsilat | 400 TL açık bakiye |
| T-07 | 2 farklı dersin toplam 2.000 TL'si tek ödemeyle kapatılır | Her ikisi kapalı, çift sayım yok |
| T-08 | Öğrenci saatlik ücreti değiştirir | Eski ders snapshot değişmez |
| T-09 | Sabit ders bulunan hafta için taslak | Sabit ders hareket etmez |
| T-10 | Taslak sonrası manuel çakışan ders eklenir | Onay 409 ile durur, değişiklik olmaz |
| T-11 | Taslak onayı iki kez gönderilir | Aynı dersler tekrar oluşturulmaz |
| T-12 | Kaydetme sırasında 1 kayıt başarısız olur | Transaction rollback |
| T-13 | Taslak farklı öğretmene ait | Onay reddedilir |
| T-14 | Geçmiş ders ödeme bağlıyken otomatik taşınmak istenir | Taşıma reddedilir / sabit sayılır |
| T-15 | Ders sonunda kullanıcı işlem yapmaz | Yoklama `Pending`; otomatik tahakkuk oluşmaz |
| T-16 | Öğrenci arşivlenir | Geçmiş ders ve tahsilat geçmişi görünür kalır |

## Test katmanları

- **Unit test:** Para hesabı, durum geçişleri, slot/kısıt doğrulama, optimizasyon puanı.
- **Integration test:** EF Core işlemleri, transaction, filtreler, Identity ve tenant izolasyonu.
- **UI/end-to-end:** Öğrenci ekle → ders planla → tamamla → tahsilat gir → alacak görüntüle.
- **Eşzamanlılık testi:** Aynı öğretmenin takvimine paralel ders ekleme ve taslak onaylama.

## Veri koruma kontrol listesi

- Erişim kontrolleri ve gereksiz veri toplamama ilkesi
- Test verilerinde gerçek öğrenci telefon numarası kullanmama
- Gizli anahtarları `.env`/secret manager ile saklama (Git'e koymama)
- Şifre sıfırlamada güvenli token ve süre sonu
- Kayıt dışa aktarma/silme talepleri ve yasal saklama gerekliliklerini ürünleşmeden önce belirleme
- Audit log içinde gereksiz hassas veri tutmama

**Not:** KVKK uyumu yalnızca teknik önlemlerden ibaret değildir; gerçek kullanıma açılmadan önce uygun hukuki/operasyonel değerlendirme gerekir.
