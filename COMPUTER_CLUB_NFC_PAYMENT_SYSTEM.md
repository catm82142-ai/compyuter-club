# Computer Club NFC Payment System — Loyiha Tavsifi

## 1) Maqsad

Computer club (gaming club) ichidagi har bir kompyuter uchun alohida NFC sticker qo‘yiladi. Foydalanuvchi NFC’ga telefonini tekkizganda avtomatik web sahifa ochiladi va aynan o‘sha kompyuter uchun:
- vaqt tanlaydi
- to‘lov qiladi (Payme/Click)
- to‘lov tasdiqlangach kompyuter sessiyasi avtomatik boshlanadi
- vaqt tugagach sessiya avtomatik to‘xtaydi va kompyuter qayta “lock/kiosk” holatga qaytadi

Telefon bo‘lmasa yoki karta orqali to‘lov qilolmasa:
- foydalanuvchi “Telefonim yo‘q” yoki “Naqd” variantini tanlaydi
- admin panelda so‘rov ko‘rinadi
- admin naqd pulni qabul qilib tasdiqlaydi va sessiya yoqiladi

## 2) Asosiy komponentlar

### 2.1 NFC sticker (har PC uchun)
- NFC ichida URL bo‘ladi: `https://<domain>/p/<pcId>`
- `pcId` har bir kompyuterga unik bo‘ladi (masalan: `PC-01`, `PC-02`)

### 2.2 Web ilova (foydalanuvchi sahifasi)
Vazifalari:
- PC sahifasini ochish (`pcId` bo‘yicha)
- foydalanuvchini identifikatsiya qilish (telefon + SMS OTP)
- PC kodi (ekranda chiqadigan kod)ni kiritish
- tarif/vaqt tanlash va invoice yaratish
- Payme/Click orqali to‘lovni yakunlash
- sessiya holatini ko‘rsatish (qolgan vaqt, tugash vaqti)

### 2.3 Admin panel
Vazifalari:
- real-time PC holati (bo‘sh/band/offline)
- naqd so‘rovlar ro‘yxati (qaysi PC, qachon, so‘ralgan vaqt)
- naqd so‘rovni tasdiqlash/inkor qilish
- sessiyalar ro‘yxati va tarix
- operatorlar (admin) boshqaruvi (keyingi bosqich)
- audit log (kim qachon nima qildi)

### 2.4 PC Agent (har bir PC’da ishlaydigan dastur)
Vazifalari:
- PC doim yoqilgan bo‘ladi, lekin ekranda “kiosk/lock” sahifa turadi
- serverdan buyruqlarni oladi: `startSession`, `stopSession`, `lock`
- sessiya bo‘lsa foydalanuvchiga ruxsat beradi (unlock yoki launcher rejimi)
- vaqt tugaganda avtomatik lock holatga qaytaradi
- serverga heartbeat yuboradi: online/offline, CPU/ram ixtiyoriy, oxirgi sessiya holati

## 3) Asosiy oqimlar

### 3.1 NFC → to‘lov → sessiya start (karta to‘lovi)
1) Foydalanuvchi NFC’ga tegadi → web sahifa ochiladi (`/p/<pcId>`)
2) Foydalanuvchi telefon raqamini kiritadi va SMS OTP bilan tasdiqlaydi
3) Foydalanuvchi PC ekranidagi 4–6 xonali “PC kodi”ni telefondagi sahifaga kiritadi
4) Vaqt tanlaydi (masalan: 30/60/120 daqiqa)
5) Web ilova backend orqali Payme/Click invoice yaratadi
6) Payme/Click to‘lov tasdiqlansa backend sessiyani `paid` qiladi
7) Backend PC Agentga `startSession(duration)` yuboradi
8) PC Agent sessiyani boshlaydi, vaqtni kuzatadi va tugaganda `stopSession` qiladi

### 3.2 Naqd yoki telefon yo‘q oqimi
1) PC kiosk ekranda “Telefonim yo‘q” tugmasi bosiladi (yoki web sahifada “Naqd” tanlanadi)
2) Backend admin panelga “cash_request” yuboradi (pcId bilan)
3) Foydalanuvchi admin yoniga borib naqd to‘lov qiladi
4) Admin panelda so‘rovni tasdiqlaydi (vaqt/summa tekshiriladi)
5) Backend PC Agentga `startSession(duration)` yuboradi

## 4) Xavfsizlik modeli (tavsiya qilingan)

### 4.1 PC kodi (anti-remote)
Muammo: foydalanuvchi klubdan ketib, keyin link orqali masofadan sessiya ochib yubormasligi kerak.

Yechim:
- Har PC kiosk ekranida 4–6 xonali kod doim ko‘rinadi
- Kod 30–60 soniyada yangilanadi (TTL)
- To‘lovga o‘tishdan oldin foydalanuvchi shu kodni telefonda kiritadi
- Backend kodni `pcId + TTL` bo‘yicha tekshiradi; mos kelmasa to‘lovni bloklaydi

### 4.2 Sessiya tokenlari
- Web ilova sessiya yaratishda short-lived token ishlatadi (1–2 daqiqa)
- Token faqat bitta `pcId` uchun amal qiladi

### 4.3 Rate-limit va antifraud
- OTP request va verify uchun limitlar (raqam/IP bo‘yicha)
- to‘lov yaratish uchun limit (bir PC uchun bir vaqtning o‘zida bitta pending invoice)
- admin tasdiqlari audit logga yoziladi

## 5) To‘lov integratsiya (Payme/Click)

### 5.1 Asosiy talablar
- invoice/order yaratish
- callback (payment status) qabul qilish
- statusni server tomonida final qilish (faqat callback bo‘yicha)

### 5.2 Muhim qoidalar
- “paid” holatini faqat Payme/Click callback tasdiqlaydi
- to‘lov bo‘lmasa sessiya yoqilmaydi (naqd bo‘lsa admin tasdiqlaydi)

## 6) Ma’lumotlar modeli (taklif)

### 6.1 pcs
- `pcId` (unique)
- `name` (masalan: “PC 01”)
- `status`: `online|offline`
- `lastHeartbeatAt`
- `createdAt`, `updatedAt`

### 6.2 kiosk_codes
- `pcId`
- `codeHash`
- `expiresAt` (TTL index)
- `createdAt`

### 6.3 users
- `phone` (unique)
- `createdAt`, `updatedAt`

### 6.4 sessions
- `sessionId` (unique)
- `pcId`
- `userId` (naqd bo‘lsa ixtiyoriy)
- `status`: `pending_payment|paid|active|ended|canceled`
- `durationMinutes`
- `price`
- `startsAt`, `endsAt`
- `createdAt`, `updatedAt`

### 6.5 payments
- `provider`: `payme|click`
- `providerPaymentId`
- `sessionId`
- `status`: `created|paid|failed|canceled`
- `amount`
- `createdAt`, `updatedAt`

### 6.6 cash_requests
- `requestId`
- `pcId`
- `requestedMinutes`
- `status`: `pending|approved|rejected|expired`
- `approvedByAdminId` (ixtiyoriy)
- `createdAt`, `updatedAt`

### 6.7 audit_logs
- `actorType`: `admin|system`
- `actorId`
- `action`
- `payload` (json)
- `createdAt`

## 7) Backend API (taklif)

### 7.1 Public (user)
- `GET /p/:pcId` — PC sahifa metasi (tariflar, holat)
- `POST /auth/request-otp` — SMS OTP yuborish
- `POST /auth/verify-otp` — OTP tasdiqlash, token qaytarish
- `POST /kiosk/verify-code` — `pcId + code` tekshirish
- `POST /payments/create` — invoice yaratish (`pcId`, `minutes`, `verifiedCodeToken`)
- `POST /payments/callback/payme` — Payme callback
- `POST /payments/callback/click` — Click callback

### 7.2 Admin
- `POST /admin/login`
- `GET /admin/pcs` — PC holatlari
- `GET /admin/cash-requests`
- `POST /admin/cash-requests/:id/approve`
- `POST /admin/cash-requests/:id/reject`
- `GET /admin/sessions`

### 7.3 PC Agent
- `POST /agent/register` — agent birinchi marta ulanishi (pcId, secret)
- `POST /agent/heartbeat` — online holat
- `WS /agent/ws` — real-time buyruqlar: start/stop/lock

## 8) PC Agent UX (kiosk ekran)
- Ekranda aniq ko‘rsatma: “NFC’ga telefonni tekkizing va to‘lov qiling”
- “Telefonim yo‘q” tugmasi
- PC kodi (katta, ko‘rinadigan)
- Sessiya bo‘lsa: qolgan vaqt ko‘rsatkich (ixtiyoriy)

## 9) MVP scope (birinchi ishga tushadigan minimal variant)
- NFC URL ochilishi va pcId bo‘yicha sahifa
- Telefon + SMS OTP
- PC kodi tekshiruvi
- Payme/Click to‘lov + callback
- Session start/stop buyruqlari
- PC Agent kiosk/lock + timer
- Admin panel: naqd so‘rovni ko‘rish va tasdiqlash

## 10) Keyingi bosqich g‘oyalar
- Tariflar: paketlar, bonus daqiqa, abonement
- Foydalanuvchi balans (deposit)
- Multi-branch (bir nechta club)
- Operator rollari va ruxsatlar
- Monitoring: agent loglari, offline alert
- “Qo‘shimcha vaqt” (sessiya tugashidan oldin 5 daqiqa qolsa telefon orqali uzaytirish)

