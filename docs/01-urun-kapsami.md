> **Güncelleme (2026-11-22):** Genel kapsam için `02-fonksiyonel-gereksinimler.md`, geçerli geliştirme planı için `09-yol-haritasi.md` esas alınır. Bu belgedeki örnek mimari ve API kararları taslaktır.

# 01 — Ürün kapsamı

## Ürün vizyonu

Özel ders veren tekil öğretmenlerin öğrenci portföyünü, derslerini ve parasal kayıtlarını güvenilir biçimde yönetebilmesi; uygunluklara dayanarak haftalık programını hızlı hazırlayabilmesi.

## Birincil kullanıcı

- **Öğretmen:** Kayıt olur, öğrencilerini yönetir, haftalık takvimini düzenler, ders sonunda katılım/ücret/ödeme kaydeder, raporları görür.
- İlk sürümde **öğrenci hesabı, veli hesabı, kurum yöneticisi rolü yoktur**. Öğrenci yalnızca öğretmenin yönettiği veri kaydıdır.

## Hedefler

1. Günlük takip yükünü azaltmak.
2. Çakışmasız haftalık program üretmek; son kararı öğretmene bırakmak.
3. Eksik ders tamamlama kayıtlarını görünür tutmak.
4. Tahakkuk (ödenecek ücret) ile tahsilatı (alınan para) karıştırmamak.
5. Masaüstü ve telefon üzerinde kolay kullanılmak.

## Kesinleşen iş kararları

| Karar | Tanım |
|---|---|
| Devamsızlık ücreti | Öğrenci gelmediğinde veya ders iptal olduğunda ücret doğup doğmayacağına **her ders için öğretmen** karar verir. |
| Planlama ufku | Otomatik programlama **tek bir hafta** (pazartesi–pazar). |
| Mevcut dersler | Öğretmen planlama öncesinde **sabit** ve **değiştirilebilir** dersleri seçer. |
| Hesaplar | İlk aşamada **yalnızca öğretmen girişi**. |
| Ders türü ve varsayılan ders | Öğretmen kayıt olurken en az bir ders türü tanımlar; ilk seçtiği ders otomatik olarak **varsayılan ders** olur. Öğretmen sonradan yeni ders türleri ekleyebilir ve varsayılan dersi değiştirebilir. |
| Öğrenci dersleri ve oturumlar | Bir öğrenciye farklı ücret ve haftalık hedeflerle **birden çok ders türü (`StudentSubjects`)** atanabilir (varsayılan ders otomatik seçili gelir); ancak **her ders oturumu (`Lessons`) yalnızca tek bir ders türüne** ait olabilir. |
| Ödeme | Öğretmen dersten sonra **alındı / alınmadı / kısmen alındı** bilgisini işleyebilir; sonradan ödeme eklenebilir. |
| Onay | Taslak onaylanmadan gerçek dersler değişmez. |

## Başlangıç varsayımları (henüz kesin karar değil)

- Tek öğretmen kendi takvimini yönetir; aynı hesap altında birden çok öğretmen/şube yoktur.
- Dersler bir öğretmen ile bir öğrenci arasında bireyseldir; grup dersleri ilk sürüm dışında.
- Saatler öğretmenin seçtiği yerel saat dilimine göre görüntülenir; saklama UTC tabanlı yapılır.
- Ücret para birimi ilk sürümde TRY; çoklu para birimi kapsam dışında.
- Her öğrenci-ders türü eşleşmesi için varsayılan ders süresi, haftalık hedef ders sayısı ve saatlik ücret tanımlanabilir.
- Ödeme öğretmen tarafından manuel kaydedilir; çevrim içi ödeme tahsilat entegrasyonu yapılmaz.

## Kapsam dışı / ileri faz

- Öğrenci/veli portalı, mesajlaşma, ödeme altyapısı, online ders görüşmesi
- Birden fazla öğretmenli kurum, dershane işletmesi ve sınıf/grup dersleri
- Yapay zekâ zorunluluğu (ilk algoritma deterministiktir)
- Tek onayda birden çok haftayı otomatik planlama
- Mobil mağazalara yayınlanan native uygulama

## Başarı ölçütleri (ürün denemelerinde netleştirilecek)

- Temel ders ekleme ve güncelleme işlemlerinin anlaşılır olması
- Gözden kaçan derslerin bekleyen işlemler alanında bulunması
- Onaylanan öneride zorunlu kısıt ihlali ve veri çakışması olmaması
- Kısmi/toplu tahsilat sonrasında kalan borcun doğru hesaplanması

## Temel kavramlar

- **Ders (Lesson):** Belirli öğrenci ve ders türüyle planlanmış zaman aralığı.
- **Katılım:** Geldi, gelmedi, iptal, henüz doğrulanmadı gibi gerçekleşme bilgisi.
- **Tahakkuk (Charge):** Ders kaynaklı ücret yükümlülüğü. Dersin gerçekleşmesine bire bir bağlı değildir.
- **Tahsilat (Payment):** Öğretmenin fiilen aldığı para.
- **Ödeme dağıtımı (Allocation):** Tahsilatın bir veya daha fazla tahakkuka uygulanması.
- **Taslak:** Otomatik planlama önerisi; henüz gerçek takvimi değiştirmez.
- **Sabit ders:** Algoritmanın saatini değiştiremeyeceği ders.
- **Değişebilir ders:** Öğretmenin izin verdiği ölçüde algoritmanın yeniden konumlandırabileceği ders.
