# 02 — Fonksiyonel gereksinimler (FR-001–FR-143)

> Durum: planlanan kapsam; henüz uygulanmış veya test edilmiş özellikler değildir.

Bu dosya görüşmelerde bir araya getirilen **143 maddelik işlev listesi**dir. İş kuralları için [03](03-is-kurallari.md), faz eşlemesi için [09](09-yol-haritasi.md) ve [12](12-gereksinim-faz-eslestirmesi.md) belgelerine bakınız.

**Kesinleşen kararlar:** yalnızca öğretmen hesabı, tek haftalık otomatik plan, sabit/değiştirilebilir ders seçimi, devamsızlıkta ders bazında ücret kararı, çoklu ders türü. Diğer kullanım kolaylığı maddeleri planlanan kapsamdır; kesin ürün kararı gerektiren ayrıntılar [11 — Açık kararlar](11-acik-kararlar.md) içinde saklanır.

## Öğretmen hesap yönetimi

| ID | Gereksinim |
|---|---|
| FR-001 | Öğretmen sisteme kayıt olabilmeli. |
| FR-002 | E-posta ve şifresiyle giriş yapabilmeli. |
| FR-003 | Hesabından güvenli şekilde çıkış yapabilmeli. |
| FR-004 | Şifresini değiştirebilmeli. |
| FR-005 | Şifresini unuttuğunda sıfırlayabilmeli. |
| FR-006 | Ad, soyad ve iletişim bilgilerini düzenleyebilmeli. |
| FR-007 | Her öğretmen yalnızca kendi öğrencilerini görebilmeli. |
| FR-008 | Her öğretmen yalnızca kendi derslerini ve finansal kayıtlarını yönetebilmeli. |
| FR-009 | İlk sürümde öğrenciler için kullanıcı hesabı bulunmamalı. |

## Öğrenci yönetimi

| ID | Gereksinim |
|---|---|
| FR-010 | Öğretmen yeni öğrenci ekleyebilmeli. |
| FR-011 | Öğrencinin adını, soyadını ve gerekli iletişim bilgilerini kaydedebilmeli. |
| FR-012 | Öğrenci bilgilerini güncelleyebilmeli. |
| FR-013 | Öğrencileri listeleyebilmeli. |
| FR-014 | Öğrencileri isimlerine göre arayabilmeli. |
| FR-015 | Öğrencileri aktif veya arşivlenmiş durumuna göre filtreleyebilmeli. |
| FR-016 | Öğrenciyi arşivleyebilmeli. |
| FR-017 | Arşivlenen öğrenciyi yeniden aktif hâle getirebilmeli. |
| FR-018 | Her öğrencinin ayrı detay sayfası olmalı. |
| FR-019 | Öğrencinin geçmiş ve yaklaşan dersleri görüntülenebilmeli. |
| FR-020 | Öğrencinin ödeme geçmişi ve toplam borcu görüntülenebilmeli. |
| FR-021 | Öğretmen öğrenci hakkında özel notlar oluşturabilmeli. |

## Ders türleri ve öğrenci ücretleri

| ID | Gereksinim |
|---|---|
| FR-022 | Öğretmen birden fazla ders türü tanımlayabilmeli. |
| FR-023 | Matematik, fizik, geometri gibi ders türlerini düzenleyebilmeli. |
| FR-024 | Kullanılmayan ders türlerini kaldırabilmeli veya arşivleyebilmeli. |
| FR-025 | Bir öğrenciye birden fazla ders türü atanabilmeli. |
| FR-026 | Öğrenciye ve ders türüne özel saatlik ücret belirlenebilmeli. |
| FR-027 | Saatlik ders ücreti sonradan değiştirilebilmeli. |
| FR-028 | Öğrencinin haftalık kaç ders alması gerektiği belirlenebilmeli. |
| FR-029 | Farklı ders türleri için farklı ders süreleri belirlenebilmeli. |
| FR-030 | Sonradan yapılan ücret değişiklikleri geçmiş derslerin ücretlerini etkilememeli. |

## Haftalık ders programı

| ID | Gereksinim |
|---|---|
| FR-031 | Öğretmen haftalık ders çizelgesini görüntüleyebilmeli. |
| FR-032 | Önceki haftaya geçebilmeli. |
| FR-033 | Sonraki haftaya geçebilmeli. |
| FR-034 | Belirli bir haftaya doğrudan gidebilmeli. |
| FR-035 | Takvim üzerinden manuel ders ekleyebilmeli. |
| FR-036 | Dersin öğrencisini seçebilmeli. |
| FR-037 | Ders türünü seçebilmeli. |
| FR-038 | Ders tarihi, başlangıç saati ve süresini belirleyebilmeli. |
| FR-039 | Mevcut dersin bilgilerini güncelleyebilmeli. |
| FR-040 | Dersi başka bir gün veya saate taşıyabilmeli. |
| FR-041 | Dersi iptal edebilmeli. |
| FR-042 | Uygun koşullarda ders kaydını kaldırabilmeli. |
| FR-043 | Sistem aynı saat aralığındaki ders çakışmalarını engellemeli. |
| FR-044 | Takvimdeki derslere tıklanarak detayları açılabilmeli. |
| FR-045 | Ders durumları takvim üzerinde ayırt edilebilmeli. |
| FR-046 | Tekrarlayan haftalık dersler oluşturulabilmeli. |
| FR-047 | Tekrarlayan derslerin yalnızca belirli haftalardaki kayıtları değiştirilebilmeli. |

## Öğretmen ve öğrenci müsaitlikleri

| ID | Gereksinim |
|---|---|
| FR-048 | Öğretmen çalışacağı günleri belirleyebilmeli. |
| FR-049 | Günlük çalışma başlangıç ve bitiş saatlerini belirleyebilmeli. |
| FR-050 | Çalışmak istemediği günleri işaretleyebilmeli. |
| FR-051 | Günlük maksimum ders süresini belirleyebilmeli. |
| FR-052 | Dersler arasındaki minimum mola süresini belirleyebilmeli. |
| FR-053 | Öğrencilerin müsait oldukları günleri kaydedebilmeli. |
| FR-054 | Öğrencilerin uygun saat aralıklarını belirleyebilmeli. |
| FR-055 | Öğrencilerin müsaitlikleri sonradan güncellenebilmeli. |
| FR-056 | Yalnızca belirli bir hafta için geçici müsaitlik değişiklikleri yapılabilmeli. |

## Yoklama ve ders tamamlama

| ID | Gereksinim |
|---|---|
| FR-057 | Öğretmen dersin gerçekleşip gerçekleşmediğini işaretleyebilmeli. |
| FR-058 | Öğrencinin derse gelip gelmediğini belirleyebilmeli. |
| FR-059 | Ders iptal edildi veya ertelendi olarak işaretlenebilmeli. |
| FR-060 | Ders saati geçince ders otomatik tamamlandı sayılmamalı. |
| FR-061 | Yoklaması girilmeyen dersler doğrulama bekliyor durumunda kalmalı. |
| FR-062 | Öğretmen geçmiş derslerin yoklamasını sonradan tamamlayabilmeli. |
| FR-063 | Gün sonunda eksik yoklamalar topluca görüntülenebilmeli. |
| FR-064 | Öğretmen birden fazla dersin yoklamasını topluca tamamlayabilmeli. |
| FR-065 | Öğrenci gelmezse ücret alınıp alınmayacağını öğretmen her ders için ayrı belirleyebilmeli. |
| FR-066 | Öğrencinin gelmediği derslerden otomatik borç oluşturulmamalı. |
| FR-067 | Öğretmenin seçimine göre devamsızlık ücreti oluşturulabilmeli. |
| FR-068 | Öğretmen ücretlendirme kararını kontrollü şekilde düzeltebilmeli. |

## Ödeme ve borç yönetimi

| ID | Gereksinim |
|---|---|
| FR-069 | Her dersin ücret bilgisi ayrı saklanmalı. |
| FR-070 | Ders için ücret oluşup oluşmadığı takip edilmeli. |
| FR-071 | Öğretmen öğrenciden ödeme aldığını kaydedebilmeli. |
| FR-072 | Hiç ödeme alınmadı durumu kaydedilebilmeli. |
| FR-073 | Kısmi ödeme yapılabilmeli. |
| FR-074 | Birden fazla ders için toplu ödeme kaydedilebilmeli. |
| FR-075 | Öğrencinin kalan borcu otomatik hesaplanmalı. |
| FR-076 | Öğrencinin ödeme geçmişi görüntülenebilmeli. |
| FR-077 | Ödemeler tarihe ve öğrenciye göre filtrelenebilmeli. |
| FR-078 | Öğretmen toplam tahsilatını görüntüleyebilmeli. |
| FR-079 | Toplam alacaklar ayrı görüntülenebilmeli. |
| FR-080 | Hatalı ödeme kayıtları kontrollü şekilde düzeltilebilmeli. |
| FR-081 | Ödeme değişikliklerinin geçmişi korunmalı. |
| FR-082 | Öğretmen dersin yoklamasını tamamlayıp ödeme bilgisini daha sonra da girebilmeli. |

## Otomatik ders programı oluşturma

| ID | Gereksinim |
|---|---|
| FR-083 | Öğretmen otomatik program oluşturma ekranını açabilmeli. |
| FR-084 | Programın oluşturulacağı haftayı seçebilmeli. |
| FR-085 | Algoritma yalnızca seçilen bir hafta için çalışmalı. |
| FR-086 | Programa dahil edilecek öğrenciler seçilebilmeli. |
| FR-087 | Öğrencilerin haftalık ders ihtiyaçları dikkate alınmalı. |
| FR-088 | Öğrencilerin müsaitlik saatleri dikkate alınmalı. |
| FR-089 | Öğretmenin çalışma saatleri dikkate alınmalı. |
| FR-090 | Ders süreleri ve mola kuralları dikkate alınmalı. |
| FR-091 | Algoritma çakışan dersler oluşturmamalı. |
| FR-092 | Öğretmen mevcut dersleri sabit veya değiştirilebilir seçebilmeli. |
| FR-093 | Sabit derslerin saatleri değiştirilmemeli. |
| FR-094 | Değiştirilebilir dersler algoritmayla yeniden planlanabilmeli. |
| FR-095 | Algoritma mümkünse öğretmenin gün içindeki gereksiz boşluklarını azaltmalı. |
| FR-096 | Oluşturulan program öğretmene taslak olarak sunulmalı. |
| FR-097 | Taslak haftalık çizelgede görüntülenebilmeli. |
| FR-098 | Öğretmen taslaktaki dersleri değiştirebilmeli. |
| FR-099 | Öğretmen alternatif bir program isteyebilmeli. |
| FR-100 | Yerleştirilemeyen dersler ve nedenleri gösterilmeli. |
| FR-101 | Öğretmen taslağı onaylayabilmeli veya reddedebilmeli. |
| FR-102 | Öğretmen onaylamadan gerçek dersler değiştirilmemeli. |
| FR-103 | Onay sırasında ders çakışmaları yeniden kontrol edilmeli. |
| FR-104 | Taslaktaki bütün değişiklikler güvenli bir veritabanı transaction işlemiyle kaydedilmeli. |
| FR-105 | Aynı programın iki kez onaylanması tekrar eden ders kayıtları oluşturmamalı. |
| FR-106 | Seçilen haftanın dışındaki dersler değiştirilmemeli. |

## Ders geçmişi

| ID | Gereksinim |
|---|---|
| FR-107 | Geçmiş dersler görüntülenebilmeli. |
| FR-108 | Dersler tarihe göre filtrelenebilmeli. |
| FR-109 | Dersler öğrenciye göre filtrelenebilmeli. |
| FR-110 | Dersler gerçekleşme durumuna göre filtrelenebilmeli. |
| FR-111 | Derslerin yoklama ve ödeme durumları ayrı gösterilmeli. |
| FR-112 | Her dersin detayları görüntülenebilmeli. |
| FR-113 | Geçmiş ders kayıtlarında kontrollü düzeltme yapılabilmeli. |

## Ana sayfa (Dashboard)

| ID | Gereksinim |
|---|---|
| FR-114 | Öğretmen uygulamaya giriş yaptığında bir ana panel görmeli. |
| FR-115 | Bugünkü dersler gösterilmeli. |
| FR-116 | Haftalık toplam ders sayısı gösterilmeli. |
| FR-117 | Aktif öğrenci sayısı gösterilmeli. |
| FR-118 | Bekleyen yoklama işlemleri gösterilmeli. |
| FR-119 | Toplam alacaklar gösterilmeli. |
| FR-120 | Yaklaşan dersler gösterilmeli. |
| FR-121 | Ana sayfadan hızlıca ders eklenebilmeli. |
| FR-122 | Ana sayfadan bekleyen ders işlemlerine geçilebilmeli. |

## Raporlama ve istatistikler

| ID | Gereksinim |
|---|---|
| FR-123 | Günlük, haftalık ve aylık ders sayıları gösterilmeli. |
| FR-124 | Toplam ders saatleri hesaplanmalı. |
| FR-125 | Aylık tahsilatlar gösterilmeli. |
| FR-126 | Toplam alacaklar raporlanmalı. |
| FR-127 | Öğrenci bazında ders geçmişi analiz edilebilmeli. |
| FR-128 | İptal edilen dersler raporlanmalı. |
| FR-129 | Devamsızlık istatistikleri görüntülenebilmeli. |
| FR-130 | Öğretmenin en yoğun çalışma günleri gösterilebilmeli. |
| FR-131 | Ders ve tahsilat grafikleri oluşturulabilmeli. |

## Bildirimler ve hatırlatmalar

| ID | Gereksinim |
|---|---|
| FR-132 | Yaklaşan dersler için hatırlatma oluşturulabilmeli. |
| FR-133 | Tamamlanmayan yoklamalar öğretmene hatırlatılabilmeli. |
| FR-134 | Gün sonunda bekleyen işlemler gösterilebilmeli. |
| FR-135 | Ödeme bekleyen öğrenciler listelenebilmeli. |
| FR-136 | Öğretmen bildirimlerini açıp kapatabilmeli. |
| FR-137 | Aynı işlem için gereksiz tekrar bildirimleri engellenmeli. |

## Öğretmen ayarları

| ID | Gereksinim |
|---|---|
| FR-138 | Varsayılan ders süresi belirlenebilmeli. |
| FR-139 | Dersler arası minimum mola süresi ayarlanabilmeli. |
| FR-140 | Günlük maksimum ders süresi belirlenebilmeli. |
| FR-141 | Çalışma günleri ve saatleri değiştirilebilmeli. |
| FR-142 | Bildirim tercihleri düzenlenebilmeli. |
| FR-143 | Hesap ve güvenlik ayarları yönetilebilmeli. |
