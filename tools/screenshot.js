/* tools/screenshot.js — tam sayfa ekran görüntüsü, reveal'leri TETİKLEYEREK.
 *
 * NEDEN: `npx playwright screenshot` yetmiyor — IntersectionObserver ile açılan
 * bölümler kaydırılmadan opacity:0 kalır, prototip yarı boş çıkar. Bu script
 * sayfayı kademeli kaydırıp başa döner, sonra çeker.
 *
 * KULLANIM (repo kökünden):
 *   node tools/screenshot.js "file://$PWD/design_handoff_portfolio/live-html/index.html" ref-home.png
 *   node tools/screenshot.js "http://127.0.0.1:5099/" my-home.png
 *
 * ⚠️ Prototip DAİMA file:// ile açılır — HTTP'de kendi runtime'ı DOM'u bozuyor
 *    (satır 392 fetch(location.href)). Bkz. vault wiki/design-system.md.
 * Gereksinim: Node 20+ (Mac'te 26.5.0) + `npm i playwright` + `npx playwright install chromium`.
 */
const { chromium } = require('playwright');
(async () => {
  const [url, cikti] = process.argv.slice(2);
  const b = await chromium.launch();
  const p = await b.newPage({ viewport: { width: 1440, height: 900 } });
  await p.goto(url, { waitUntil: 'load' });
  await p.waitForTimeout(2500);
  const h = await p.evaluate(() => document.documentElement.scrollHeight);
  for (let y = 0; y < h; y += 700) { await p.evaluate(v => window.scrollTo(0, v), y); await p.waitForTimeout(280); }
  await p.evaluate(() => window.scrollTo(0, 0));
  await p.waitForTimeout(1400);
  await p.screenshot({ path: cikti, fullPage: true });
  await b.close();
  console.log('✓', cikti);
})();
