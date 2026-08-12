/* tools/olc.js — prototip ↔ implementasyon SAYISAL karşılaştırma.
 * Göz yerine ölçüm: hero yüksekliği, içerik kutusu genişliği, menü yüksekliği,
 * toplam sayfa yüksekliği. Faz 1 sonunda dördü de birebir eşleşti (4784px).
 * Kullanım: uygulama :5099'da ayaktayken `node tools/olc.js`
 */
const { chromium } = require('playwright');
const path = require('path');
// Yol depoya göre çözülür: mutlak yol yazınca hem başka makinede kırılıyor
// hem de kullanıcı adı/dizin düzeni herkese açık depoya sızıyordu.
const PROTOTIP = 'file://' + path.resolve(__dirname, '..',
  'design_handoff_portfolio/live-html/index.html');
(async () => {
  const b = await chromium.launch();
  for (const [ad, url] of [
    ['PROTOTİP', PROTOTIP],
    ['BENİM   ', 'http://127.0.0.1:5099/']
  ]) {
    const p = await b.newPage({ viewport: { width: 1440, height: 900 } });
    await p.goto(url); await p.waitForTimeout(3000);
    const r = await p.evaluate(() => {
      const hero = document.querySelector('#top');
      const h1 = document.querySelector('#top h1');
      const nav = document.querySelector('nav');
      const box = h1 ? h1.getBoundingClientRect() : null;
      return {
        heroYukseklik: hero ? Math.round(hero.getBoundingClientRect().height) : null,
        heroSol: box ? Math.round(box.left) : null,
        heroIcerikGenislik: hero ? Math.round(hero.clientWidth - parseFloat(getComputedStyle(hero).paddingLeft) * 2) : null,
        navYukseklik: nav ? Math.round(nav.getBoundingClientRect().height) : null,
        sayfaYukseklik: document.documentElement.scrollHeight
      };
    });
    console.log(ad, JSON.stringify(r));
    await p.close();
  }
  await b.close();
})();
