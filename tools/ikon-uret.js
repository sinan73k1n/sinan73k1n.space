#!/usr/bin/env node
/*
  ikon-uret.js — site ikonu + og:image üretici (Faz 9.5 / 7.4)

  NEDEN SCRIPT: tek kaynak `wwwroot/icon.svg`. PNG/ICO/OG kartı ondan TÜRETİLİR;
  elle çizilmiş ikinci bir dosya olursa palet değişince biri güncellenmeden kalır.
  İkon veya renk değişince bu script tekrar çalıştırılır, üretilen dosyalar depoya girer
  (çalışma zamanında node yok — sunucuda Node kurulu DEĞİL, bilinçli).

  Gereksinim (yalnız Mac, geliştirme): node 20+ ve `npx playwright` (chromium-headless-shell).
  Çalıştırma:  node tools/ikon-uret.js

  Üretilenler → src/Portfolio.SITE_UI/wwwroot/
    favicon.ico (16+32+48, PNG gömülü) · apple-touch-icon.png (180, tam taşma)
    icon-192.png · icon-512.png · og-image.png (1200×630)
*/
const fs = require('fs');
const os = require('os');
const path = require('path');
const { execFileSync } = require('child_process');

const KOK = path.resolve(__dirname, '..');
const WWW = path.join(KOK, 'src/Portfolio.SITE_UI/wwwroot');
const GECICI = fs.mkdtempSync(path.join(os.tmpdir(), 'ikon-'));

const ikonSvg = fs.readFileSync(path.join(WWW, 'icon.svg'), 'utf8');
const yorumsuz = ikonSvg.replace(/<!--[\s\S]*?-->/g, '');

/** Bir HTML'i verilen boyutta ekran görüntüsüne çevirir. */
function cek(html, en, boy, cikti) {
  const dosya = path.join(GECICI, `w-${en}x${boy}.html`);
  fs.writeFileSync(dosya, html);
  execFileSync('npx', ['playwright', 'screenshot',
    `--viewport-size=${en},${boy}`, '--wait-for-timeout=500', dosya, cikti],
    { stdio: 'ignore' });
}

const sar = (icerik, ekCss = '') =>
  `<style>html,body{margin:0;padding:0}svg{display:block;width:100vw;height:100vh}${ekCss}</style>${icerik}`;

// ---- 1) İkon PNG'leri (yuvarlak köşeli, saydam dış alan) ----
const olculer = [16, 32, 48, 192, 512];
for (const n of olculer) cek(sar(yorumsuz), n, n, path.join(GECICI, `icon-${n}.png`));
fs.copyFileSync(path.join(GECICI, 'icon-192.png'), path.join(WWW, 'icon-192.png'));
fs.copyFileSync(path.join(GECICI, 'icon-512.png'), path.join(WWW, 'icon-512.png'));

// ---- 2) apple-touch-icon: TAM TAŞMA ----
// iOS köşeleri kendi maskesiyle yuvarlar; saydam köşe bırakırsak arkasında siyah görünür.
const appleSvg = yorumsuz
  .replace(/<rect x="2"[^>]*\/>/, '<rect x="0" y="0" width="64" height="64" fill="url(#yuzey)"/>')
  .replace(/<rect x="3.25"[^>]*\/>/, '');
cek(sar(appleSvg), 180, 180, path.join(WWW, 'apple-touch-icon.png'));

// ---- 3) favicon.ico ----
// ICO konteyneri elle kuruluyor: makinede ico üreten araç yok (magick/icotool kurulu değil).
// Vista+ biçiminde PNG blokları doğrudan gömülebilir; BMP'ye çevirmek gerekmez.
{
  const boyutlar = [16, 32, 48];
  const pngler = boyutlar.map(n => fs.readFileSync(path.join(GECICI, `icon-${n}.png`)));
  const bas = Buffer.alloc(6);
  bas.writeUInt16LE(0, 0); bas.writeUInt16LE(1, 2); bas.writeUInt16LE(boyutlar.length, 4);
  let ofset = 6 + 16 * boyutlar.length;
  const girdiler = boyutlar.map((n, i) => {
    const g = Buffer.alloc(16);
    g[0] = n; g[1] = n;
    g.writeUInt16LE(1, 4); g.writeUInt16LE(32, 6);
    g.writeUInt32LE(pngler[i].length, 8); g.writeUInt32LE(ofset, 12);
    ofset += pngler[i].length;
    return g;
  });
  fs.writeFileSync(path.join(WWW, 'favicon.ico'), Buffer.concat([bas, ...girdiler, ...pngler]));
}

// ---- 4) og:image (1200×630) ----
// ⚠️ Metin DİLDEN BAĞIMSIZ tutuldu (ad + teknolojiler + adres). Site TR/EN/RU;
// karta çevrilebilir bir slogan koymak üç ayrı görsel ve üç ayrı bakım noktası demekti.
const fontVer = n => 'data:font/woff2;base64,' +
  fs.readFileSync(path.join(WWW, 'fonts', n)).toString('base64');
// Alt kümeler ayrı dosya: "-latin-ext" içinde ASCII harf YOK. Yalnız onu yüklemek
// metni sessizce yedek fonta düşürür (serif çıkar) → ikisi birden, unicode-range ile.
const EXT = 'U+0100-024F,U+0259,U+1E00-1EFF,U+2020,U+20A0-20AB,U+20AD-20CF,U+2113,U+2C60-2C7F,U+A720-A7FF';
const ogCss = `
@font-face{font-family:SG;src:url('${fontVer('space-grotesk-600-latin.woff2')}') format('woff2');font-weight:600}
@font-face{font-family:SG;src:url('${fontVer('space-grotesk-600-latin-ext.woff2')}') format('woff2');font-weight:600;unicode-range:${EXT}}
@font-face{font-family:JB;src:url('${fontVer('jetbrains-mono-400-latin.woff2')}') format('woff2')}
@font-face{font-family:JB;src:url('${fontVer('jetbrains-mono-400-latin-ext.woff2')}') format('woff2');unicode-range:${EXT}}
body{width:1200px;height:630px;background:#141622;position:relative;overflow:hidden;font-family:SG,sans-serif;color:#f2f5fc}
svg{width:78px;height:78px}
.bulut{position:absolute;border-radius:50%;filter:blur(90px)}
.b1{width:620px;height:620px;left:-140px;top:-220px;background:rgba(192,153,255,.30)}
.b2{width:560px;height:560px;right:-120px;bottom:-240px;background:rgba(0,194,219,.24)}
.b3{width:420px;height:420px;left:52%;top:44%;background:rgba(192,153,255,.13)}
.ic{position:absolute;inset:0;padding:74px 84px;display:flex;flex-direction:column;justify-content:space-between}
.ust{display:flex;align-items:center;gap:22px}
.el{font-family:JB;font-size:23px;color:#c099ff}
h1{margin:0;font-size:104px;font-weight:600;letter-spacing:-.035em;line-height:1}
.alt{margin-top:22px;font-family:JB;font-size:27px;color:#00c2db}
.dip{display:flex;justify-content:space-between;align-items:flex-end;font-family:JB;font-size:22px;color:rgba(242,245,252,.55)}
.cizgi{position:absolute;left:0;right:0;bottom:0;height:6px;background:linear-gradient(90deg,#c099ff,#00c2db)}`;

const ogHtml = sar(`
<div class="bulut b1"></div><div class="bulut b2"></div><div class="bulut b3"></div>
<div class="ic">
  <div class="ust">${yorumsuz}<span class="el">sinan.tekin</span></div>
  <div><h1>Sinan Tekin</h1><div class="alt">Unity &middot; ASP.NET Core &middot; C#</div></div>
  <div class="dip"><span>sinan73k1n.space</span><span>github.com/sinan73k1n</span></div>
</div>
<div class="cizgi"></div>`, ogCss);
cek(ogHtml, 1200, 630, path.join(WWW, 'og-image.png'));

fs.rmSync(GECICI, { recursive: true, force: true });
console.log('✓ favicon.ico · apple-touch-icon.png · icon-192/512.png · og-image.png →', WWW);
