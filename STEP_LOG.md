# Computer Club MVP Ish Jurnali

## 1-qadam

- Loyiha bo'sh ekanligi tekshirildi.
- Boshlang'ich stack sifatida `Node.js + TypeScript + Express` tanlandi.
- Maqsad: NFC kelmasidan oldin ishlaydigan minimal backend skeleti yaratish.
- Shu fayl keyingi barcha o'zgarishlar uchun ish jurnali sifatida yuritiladi.

## 2-qadam

- `package.json` yaratildi.
- `TypeScript` kompilyatsiyasi uchun `tsconfig.json` qo'shildi.
- `node_modules` va build chiqindilarini chetlab o'tish uchun `.gitignore` yaratildi.
- Endi serverning `src/` ichidagi asosiy kodlari qo'shiladi.

## 3-qadam

- `src/types.ts` ichida asosiy turlar yaratildi: `Pc`, `Session`, `Payment`, `CashRequest`, `AgentCommand`.
- `src/store.ts` ichida vaqtinchalik `in-memory` store yozildi.
- Demo uchun `PC-01`, `PC-02`, `PC-03` seed ma'lumotlari qo'shildi.
- Mock to'lov orqali sessiya yaratish, cash request yaratish, session yakunlash, agent command queue logikasi qo'shildi.
- `src/app.ts` ichida minimal API endpointlar yaratildi.
- `src/server.ts` ichida server start nuqtasi qo'shildi.
- Hozirgi skelet database ishlatmaydi; tez start va test uchun `memory` ichida ishlaydi.

## 4-qadam

- `README.md` yaratildi.
- Ishga tushirish komandalarini va birinchi `curl` testlarini yozib qo'ydim.
- Shu bilan loyiha ochilganda qaysi endpointdan boshlash kerakligi aniq bo'ldi.

## 5-qadam

- `npm install` bajarildi va dependencylar o'rnatildi.
- `npm run build` muvaffaqiyatli o'tdi.
- `npm run dev` orqali server ko'tarildi.
- `GET /health` tekshirildi va server javob qaytardi.
- `GET /p/PC-01` tekshirildi va demo PC ma'lumoti qaytdi.
- `POST /payments/mock-create` orqali test session yaratildi.
- Test natijasida `PC-01` holati `busy` bo'ldi va agent queue ichida `startSession` command hosil bo'ldi.

## 6-qadam

- `public/pc-page.html` qo'shildi va frontend test sahifa yaratildi.
- `GET /p/:pcId` endi JSON emas, oddiy HTML test UI ni ochadi.
- Sahifa ichidagi JavaScript `GET /api/pc/:pcId` orqali ma'lumot olib render qiladi.
- Test tugmalari orqali `mock payment`, `cash request`, `session end`, va `refresh` amallari frontenddan sinab ko'riladigan qilindi.
- `README.md` ichida yangi `page + api` tuzilmasi hujjatlashtirildi.

## 7-qadam

- Foydalanuvchi oynasi soddalashtirildi va texnik ma'lumotlar olib tashlandi.
- Endi sahifa ochilganda birinchi qadamda telefon raqami so'raladi.
- Telefon kiritilgandan keyin faqat `kompyuter raqami` va `to'lov tariflari` ko'rsatiladi.
- `To'lash` bosilganda telefon raqami bilan birga `mock payment` yuboriladi.
- Muvaffaqiyatli to'lovdan keyin qisqa yakuniy oynada kompyuter, telefon, vaqt va summa ko'rsatiladi.

## 8-qadam

- NFC stikerlar keldi, shuning uchun oqim haqiqiy klub ishiga moslandi.
- Public `POST /payments/mock-create` olib tashlandi — endi pul tekshirilmasdan sessiya boshlanmaydi.
- Mijoz sahifasi buyurtma oqimiga o'tdi: telefon → vaqt → `Kassaga yuborish` → `POST /cash-requests`; sahifa kassir tasdig'ini 3 sekundda bir tekshirib turadi.
- Bir PC uchun bitta ochiq so'rov saqlanadi: mijoz vaqtni o'zgartirsa yangi so'rov emas, o'sha so'rov yangilanadi.
- Kassa paneliga `Kassaga tushgan so'rovlar` bo'limi qo'shildi: PC, telefon va daqiqa ko'rinadi; `Tasdiqlash va ochish` (o'yin tanlash bilan) yoki `Rad etish`.
- Admin ochgan sessiya ham `cash` to'lov yozuvi bilan saqlanadigan bo'ldi (`startSessionDirect` umumiy logikaga o'tdi).
- Tasdiqlangan yoki rad etilgan so'rov qayta o'zgartirilmaydi.
- Build va 2 ta test skripti (15 tekshiruv) o'tdi; HTTP orqali `so'rov → tasdiqlash → agent buyrug'i` zanjiri tekshirildi.
- `README.md` yangi oqimga moslandi.

## 9-qadam

- Deploy uchun `railway.json` va `.railwayignore` yaratildi; `.gitignore` ga lokal `.dotnet` SDK va C# build papkalari qo'shildi.
- `package.json` ga `engines.node >= 20` qo'shildi (Railway build uchun).
- Server endi `PORT` muhit o'zgaruvchisini o'qiydi (Railway buni o'zi beradi).
- Ochiq serverda kassa panelini himoyalash uchun `ADMIN_KEY` qo'shildi: `/admin/*` va `/sessions/*` kalit bo'lmasa `401` qaytaradi.
- `app.set("trust proxy", true)` qo'shildi — reverse proxy ortida QR ichida `https` manzil to'g'ri chiqadi.
- `public/admin.html` ga parol ekrani qo'shildi: parol `localStorage` da saqlanadi, har bir so'rovga `x-admin-key` sarlavhasi qo'shiladi, noto'g'ri parolda `401` da yana login oynasi chiqadi.
- Kalitsiz (`ADMIN_KEY` yo'q) rejimda panel avvalgidek ochiq ishlaydi.
- Tekshiruv: kalitsiz/noto'g'ri kalitbilan `/admin/pcs` → `401`, to'g'ri kalit bilan → PC ro'yxati `200`, `/admin` sahifasi → `200`.
