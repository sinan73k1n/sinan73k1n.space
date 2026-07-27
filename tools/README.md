# tools — tasarım doğrulama araçları

Prototip (`design_handoff_portfolio/live-html/`) ile implementasyonu **ölçerek** karşılaştırır.
Göz kararı sapmayı gizler: Faz 1'de bu araçlar 3 sapma yakaladı (box-sizing ×2, global line-height).

## Kurulum (bir kez)
```bash
cd tools
npm install
npx playwright install chromium
```
Gereksinim: **Node 20+** (Playwright şartı).

## Kullanım
Uygulama ayaktayken (`dotnet run --project src/Portfolio.SITE_UI --urls http://127.0.0.1:5099`):

```bash
node olc.js                     # hero/menü/içerik genişliği/sayfa yüksekliği — sayısal karşılaştırma
node screenshot.js "http://127.0.0.1:5099/" my-home.png
node screenshot.js "file://$PWD/../design_handoff_portfolio/live-html/index.html" ref-home.png
```

## ⚠️ Tuzaklar
- **Prototip yalnız `file://` ile açılır.** HTTP'den servis edilirse kendi runtime'ı
  (`fetch(location.href)`) DOM'u bozar, sayfa ham JS metnine döner.
- `npx playwright screenshot` tek başına YETMEZ: IntersectionObserver ile açılan bölümler
  kaydırılmadan `opacity:0` kalır. `screenshot.js` sayfayı kademeli kaydırıp öyle çeker.
