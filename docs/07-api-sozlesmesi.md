> **Güncelleme (2026-11-22):** Genel kapsam için `02-fonksiyonel-gereksinimler.md`, geçerli geliştirme planı için `09-yol-haritasi.md` esas alınır. Bu belgedeki örnek mimari ve API kararları taslaktır.

# 07 — HTTP/API sözleşmesi (öneri)

**Durum:** Taslak. Web UI MVC kullanabilir; bu yollar ileride mobil istemci için REST API olarak geliştirilebilir. Üretim sözleşmesi değildir.

## Genel ilkeler

- Kimlik doğrulama/CSRF koruması kullanılan kimlik modeline göre uygulanır.
- `TeacherId` istemcinin keyfi olarak belirlediği alan olmaz; yetkili kullanıcıdan türetilir.
- Tarihler API'de açık ISO-8601 formatında, saat dilimi kararı belirlenmiş şekilde iletilir.
- Kimlikler tahmin edilebilir olsa bile her erişimde sahiplik denetlenir.
- Yazma işlemlerinde sunucu doğrulaması, sürüm/eşzamanlılık yönetimi ve anlamlı hata yanıtı vardır.

## Önerilen endpoint'ler

| Method | Yol | Amaç |
|---|---|---|
| GET/POST | `/api/students` | Öğrencileri listele / ekle |
| GET/PATCH | `/api/students/{id}` | Detay / düzenle |
| POST | `/api/students/{id}/archive` | Arşivle |
| GET/POST | `/api/subjects` | Ders türü listele / ekle |
| PUT | `/api/students/{id}/subjects/{subjectId}` | Öğrenciye özel ücret, hedef ders, süre |
| GET/PUT | `/api/students/{id}/availability` | Müsaitlik oku / güncelle |
| GET | `/api/lessons?weekStart=2026-11-02` | Haftalık dersler |
| POST | `/api/lessons` | Manuel ders oluştur |
| PATCH | `/api/lessons/{id}` | Ders düzenle |
| POST | `/api/lessons/{id}/complete` | Yoklama + ücret kararı |
| POST | `/api/payments` | Tahsilat ve dağıtımlar |
| GET | `/api/students/{id}/balance` | Açık alacak özeti |
| POST | `/api/schedule-drafts` | Tek haftalık taslak üret |
| GET | `/api/schedule-drafts/{id}` | Taslağı ve eksikleri oku |
| PATCH | `/api/schedule-drafts/{id}` | Taslak düzenle/sürüm artır |
| POST | `/api/schedule-drafts/{id}/approve` | Taslağı atomik onayla |
| POST | `/api/schedule-drafts/{id}/discard` | Taslağı reddet |

## Örnek istek — taslak üret

```json
{
  "weekStart": "2026-11-02",
  "studentSubjectIds": [13, 19],
  "fixedLessonIds": [101, 102],
  "movableLessonIds": [109],
  "preferences": {"minimizeGaps": true, "preferBalancedDays": false}
}
```

## Örnek yanıt — taslak

```json
{
  "draftId": 45,
  "version": 1,
  "status": "Draft",
  "weekStart": "2026-11-02",
  "placements": [
    {"studentId": 7, "subjectId": 3, "action": "New", "existingLessonId": null,
     "start": "2026-11-02T10:00:00+03:00", "end": "2026-11-02T11:00:00+03:00"}
  ],
  "unscheduled": [],
  "warnings": []
}
```

## Örnek istek — taslağı onayla

```json
{
  "expectedVersion": 1,
  "idempotencyKey": "a60ea832-3dd8-4ea2-87f9-6955ba6cfd39"
}
```

## Örnek istek — ders tamamlama

```json
{
  "attendance": "Absent",
  "feeDecision": "Charge",
  "chargeAmount": 1000.00,
  "note": "Öğretmen bu derste ücret alınmasına karar verdi."
}
```

Tahsilat ayrı endpoint ile yapılır; ders tamamlama ekranı dilerse iki ardışık işlemi orkestre edebilir. İki işlem birlikte sunulacaksa kısmi hatada yanlış finansal durum oluşmaması için tasarım ayrıca yapılır.

## Hata şeması

```json
{
  "type": "schedule_conflict",
  "message": "Taslak oluşturulduğundan beri bu saate başka bir ders eklenmiş.",
  "details": [{"lessonId": 221, "start": "2026-11-02T10:00:00+03:00"}]
}
```

Önerilen kodlar: `400` geçersiz veri, `401` oturum yok, `403` yetki yok, `404` erişilebilir kayıt bulunamadı, `409` eşzamanlılık/çakışma, `422` kısıt karşılanamadı, `500` beklenmeyen hata. Hata içeriği kişisel veya hassas detay sızdırmamalı.
