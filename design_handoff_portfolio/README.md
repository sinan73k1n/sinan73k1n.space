# Handoff: Sinan Tekin — Portfolyo Sitesi + Admin Panel

## Overview
Kişisel portfolyo sitesi (tek uzun ana sayfa, 3 dil) ve içeriğini kod satırına dokunmadan
yönetmeye izin veren bir admin panel. Site dört ana içerik alanı yayınlar:
teknolojiler/yetenekler, Google Play oyunları, çalıştırılabilir web demoları
(`demo/ServerAdminPanel`, `demo/ForumEtkilesim` gibi), ve GitHub depo ön izlemeleri.

Admin panel bütün bu içerikleri CRUD ile yönetir; demolar tek dosya HTML/JS olarak
yapıştırılır ve sitede tam ekran iframe içinde çalıştırılır.

> Bu belge Türkçe/İngilizce karışık okunabilir; kod ve alan adları İngilizce, arayüz kopyası Türkçedir.

## About the Design Files
Bu pakettteki HTML dosyaları **tasarım referansıdır** — hedeflenen görünüm ve davranışı
gösteren prototiplerdir, doğrudan production'a kopyalanacak kod değildir.
Görev: bu tasarımları hedef kod tabanının kendi ortamında (React/Next.js, Vue, Astro, vb.)
o projenin yerleşik desenleriyle **yeniden üretmek**. Henüz bir ortam yoksa,
proje için en uygun framework seçilip tasarımlar orada kurulmalıdır
(öneri: Next.js App Router + TypeScript; içerik JSON dosyası veya küçük bir DB ile).

Dosyalar tarayıcıda doğrudan açılır (JS gerektirir, ağ gerektirmez):

| Dosya | Ne |
|---|---|
| `live-html/index.html` | **Ana sayfa — tek dosya, çift tıkla açılır, tam çalışır** |
| `live-html/admin.html` | **Admin panel — tek dosya, tam çalışır** (index.html ile karşılıklı bağlantılı) |
| `source-dc/Ana Sayfa.dc.html` | Ana sayfanın okunabilir kaynağı (şablon + logic ayrı) |
| `source-dc/Admin.dc.html` | Admin panelin okunabilir kaynağı |
| `source-dc/site-data.js` | **İçerik modeli + seed data + store yardımcıları** (tek doğruluk kaynağı) |
| `source-dc/support.js` | Prototip çalışma zamanı (yeniden üretimde gerekmez, kopyalanmasın) |

### Bu dosyalardan görsel çıkarmak (önerilen doğrulama akışı)
Kod yazarken "burası nasıl görünüyordu?" sorusunu dosyanın kendisinden yanıtla:

```bash
# tek seferlik
npx playwright install chromium

# ana sayfanın tam boy görüntüsü
npx playwright screenshot --full-page --viewport-size=1440,900 \
  "live-html/index.html" ref-home.png

# admin panel
npx playwright screenshot --viewport-size=1440,900 \
  "live-html/admin.html" ref-admin.png
```
Bölüm bölüm bakmak için `index.html` içinde `#top`, `#about`, `#stack`, `#games`,
`#demos`, `#github` çapaları vardır (`index.html#games` gibi).
Kendi implementasyonunun ekran görüntüsünü aynı viewport'ta alıp yan yana karşılaştır.

## Fidelity
**High-fidelity (hifi).** Renkler, tipografi, boşluklar, animasyon süreleri ve easing
değerleri nihaidir; aşağıda tam liste var. Piksel yakınlığı hedeflenmeli.
Tek istisna: oyun kapakları ve görseller **yer tutucudur** (degrade + mono etiket);
gerçek görseller sonradan eklenecek — yer tutucuların yerine `<Image>`/`<img>` alanı bırak.

---

## Design Tokens

CSS değişkenleri olarak tanımlanır (`:root`) ve tüm bileşenler bunları kullanır.

```css
--bg:      oklch(0.205 0.024 278);  /* derin indigo taban */
--bg-top:  oklch(0.27 0.036 288);   /* sayfa üstü radial açılma */
--surf:    oklch(0.25 0.028 280);   /* şerit, demolar bölümü, üst bar */
--panel:   oklch(0.275 0.032 285);  /* terminal / tarayıcı mockup üst yüzey */
--panel2:  oklch(0.23 0.026 275);   /* mockup alt yüzey (degrade ikinci durak) */
--acc:     oklch(0.76 0.15 300);    /* menor vurgu: mor  (birincil) */
--acc-hi:  oklch(0.88 0.1 300);     /* vurgu hover */
--acc2:    oklch(0.74 0.14 210);    /* ikincil vurgu: camgöbeği */
--glow1:   oklch(0.58 0.16 300);    /* arka plan ışık bulutu 1 */
--glow2:   oklch(0.6 0.14 205);     /* ışık bulutu 2 */
--glow3:   oklch(0.62 0.13 165);    /* ışık bulutu 3 (nane) */
```

Nötr / metin renkleri (literal, temaya bağlı değil):

| Rol | Değer |
|---|---|
| Ana metin | `oklch(0.95 0.008 268)` |
| İkincil metin | `oklch(0.7–0.74 0.02 268)` |
| Sessiz metin / mono etiket | `oklch(0.56–0.66 0.02 268)` |
| Hairline çizgi | `oklch(0.95 0.008 268 / 0.09–0.14)` |
| Yüzey dolgusu (kart) | `oklch(0.95 0.008 268 / 0.028)` |
| Kart hover dolgusu | `oklch(0.95 0.008 268 / 0.05–0.07)` |
| Başarı / yeşil (log) | `oklch(0.8 0.15 150)` |
| Uyarı / kırmızı (sil) | `oklch(0.7 0.16 25)` |

Tipografi — Google Fonts: **Space Grotesk** (400/500/600/700) + **JetBrains Mono** (400/500).
Mono; etiketler, yol adları, terminal, sayaçlar ve teknik metadata için kullanılır.

| Rol | Değer |
|---|---|
| H1 (hero) | `clamp(46px, 8.2vw, 116px)` / 600 / `line-height 0.96` / `letter-spacing -0.045em` |
| H2 (bölüm) | `clamp(28px, 4vw, 54px)` / 600 / `-0.032em` |
| Vurgu paragraf | `clamp(20px, 2.2vw, 30px)` / 500 / `1.4` |
| Gövde | `16–17px` / 400 / `1.7–1.8` |
| Kart başlığı | `20–21px` / 600 / `-0.018em` |
| Mono etiket | `10.5–12.5px`, gerekirse `letter-spacing 0.1–0.24em`, `text-transform: uppercase` |

Spacing / şekil:
- Bölüm dikey boşluk: `clamp(60px, 9vh, 150px)`; yatay padding `26px`; içerik `max-width: 1360px`.
- Radius: kart/panel `14–18px`, mockup `16–18px`, çip ve buton `999px`, küçük ikon buton `8–10px`.
- Gölge: `0 30px 70px oklch(0.12 0.03 280 / 0.45–0.55)` (mockup), buton hover `0 14px 34px color-mix(in oklab, var(--acc) 35%, transparent)`.
- Kenarlıklar hep 1px hairline; **hiçbir yerde sert kare grid/desen yok** (bilinçli karar — önceki iterasyonda reddedildi).
- Standart easing: `cubic-bezier(.16, 1, .3, 1)`; reveal süresi `0.8–1.05s`; hover geçişleri `0.2–0.3s`.

---

## Screens / Views

### 1. Ana sayfa (`live-html/index.html`)
Tek uzun sayfa; sabit üst menü, sabit arka plan katmanı, üstte scroll ilerleme çubuğu.

**Arka plan katmanı** (`position: fixed; inset: 0; z-index: 0; pointer-events: none`):
- Taban: `radial-gradient(140% 100% at 50% 0%, var(--bg-top), var(--bg) 60%)`.
- 3 daire ışık bulutu (`--glow1/2/3`, `%20–34` opaklık, `border-radius: 50%`),
  `drift1 26s`, `drift2 32s`, `drift3 38s` ease-in-out sonsuz + `hueshift 40–46s` (hafif ton kayması).
- 2 geniş ışık huzmesi: `beam 22s` ve `beam 30s` linear (opaklık ≤ %7).
- 16 adet 1px dikey "veri hattı"; her birinde `01`, `{ }`, `</>`, `10`, `fn`, `::`, `=>`
  gliflerinden biri `stream 16–38s` linear ile aşağı akar (renk `--acc`/`--acc2`, opaklık `0.24`).
  Kolon x konumları ve gecikmeler rastgele (JS ile üretilir, `data-streams` içine).
- Tüm arka plan hareketi düşük kontrastlı olmalı: dikkat çekmemeli.

**Üst menü** (fixed, `padding: 16px 26px`, `backdrop-filter: blur(16px)`,
`background: color-mix(in oklab, var(--bg) 78%, transparent)`, alt hairline):
- Sol: 26px yuvarlak `linear-gradient(140deg, var(--acc), var(--acc2))` badge + mono handle.
- Sağ: bölüm bağlantıları (yalnız `> 1000px`), küçük `admin` bağlantısı, ve
  TR/EN/RU dil anahtarı (pill konteyner, aktif olan `--acc` dolgulu, metin `--bg`).

**Hero** (`min-height: 100vh`, iki kolon `1.05fr 0.95fr` — `≤1000px` tek kolon):
- Sol: durum çipi (yeşil nokta `pulse 2.4s`), H1 iki satır — 1. satır ad,
  2. satır `--acc → --acc2` degradeli metin (`background-clip: text`).
  Satırlar `overflow: hidden` sarmalayıcı içinde `translateY(110%) → 0`,
  `1.05s cubic-bezier(.16,1,.3,1)`, `0.09s` kademeli gecikme (perde etkisi).
- Altında mono "typewriter" satırı: rol listesi sırayla yazılır/silinir
  (yazma 62ms/karakter, silme 34ms, tam yazılınca 1500ms bekleme), yanında
  `blink 1.05s step-end` imleç.
- Açıklama paragrafı (max 520px) + 2 buton (dolu `--acc` pill, hover `translateY(-2px)` + glow; ikincisi outline).
- Sağ: terminal mockup — `floaty 9s` ile hafif salınım, üstte 3 daire "trafik ışığı" +
  `~/portfolio — zsh`, gövdede satırlar sırayla `240ms` aralıkla fade+`translateY(6px)→0`.
  Satır rengi ilk karakterden türetilir: `$` beyaz, `→` `--acc2`, `✓` yeşil, `$ open` `--acc`.
  Parallax: `data-par="0.1"`.
- Sol altta mono "aşağı kaydır" + kısa degrade çizgi.

**Kayan şerit**: `--surf` %60 zemin, üst/alt hairline, `marq 38s linear` ile
mono teknoloji listesi (içerik 4 kez tekrarlanır, `-50%`'ye kayar).

**Hakkımda** (`#about`, iki kolon `0.8fr 1.2fr`): solda mono bölüm numarası + maskeden
yükselen başlık; sağda vurgu paragrafı, gövde paragrafı ve 3 sayaç kutusu.
Sayaçlar görünür olunca `0 → değer` sayar (toplam ~700ms).

**Teknolojiler** (`#stack`): pill çipler (`flex-wrap`, `gap: 12px`), her çipte
`--acc`/`--acc2` sırayla 9px nokta + ad + mono not. Sonda kesikli "+ yeni teknoloji" çipi.
Reveal: `translateY(20px) scale(0.96) → none`, kapsayıcı içinde `0.07s` kademeli.

**Oyunlar** (`#games`): `repeat(auto-fill, minmax(280px, 1fr))` kart ızgarası.
Kart: 16/10 kapak (degrade + üstten radial parlaklık + `sheen 6.5s` diyagonal ışık süpürmesi
+ sol altta mono yer tutucu etiketi), altında mono meta satırı (`01 · Android · Unity`),
başlık, açıklama. Hover: `translateY(-6px)` + kenarlık `--acc` %50.

**Demolar** (`#demos`, `--surf` %55 zemin): iki kolon `1fr 1fr`.
- Sol: **yapışkan** (`sticky; top: 92px`) tarayıcı mockup — trafik ışıkları,
  pill adres çubuğunda `sinantekin.dev/<aktif demo yolu>`, gövdede soyut arayüz iskeleti
  (avatar+başlık, 3 kutu, satır çubukları), alt kısımda degrade maske ve
  ortada "Tam ekran aç" butonu.
- Sağ: demo satırları listesi. `mouseenter` aktif demoyu değiştirir (soldaki mockup anında güncellenir),
  tıklama tam ekran overlay açar. Satırda mono yol (`demo/ServerAdminPanel`), ad, açıklama,
  etiket pill'leri ve 44px yuvarlak `↗`. Aktif satırın zemini `oklch(0.95 0.008 268 / 0.055)`,
  yolu `--acc` renginde. Sonda kesikli "boş demo alanı" satırı.
- **Tam ekran demo overlay**: `position: fixed; inset: 0; z-index: 90`. Üstte `--surf` bar
  (yeşil nokta + `sinantekin.dev/<yol>` + "Kapat ✕"), altta demo `iframe`
  (`srcdoc` = demonun HTML/JS içeriği; içerik boşsa yer tutucu stub). `Escape` kapatır.

**GitHub** (`#github`): sol tarafta dikey degrade hat; hat üzerinde 9px nokta
`travel 7s linear` ile yukarıdan aşağı iner (commit hissi). Sağda depo kartları
(`minmax(300px, 1fr)`): mono depo adı + `↗`, açıklama (min-height 46px), altta
dil noktası + dil + yıl. Hover: `translateY(-4px)` + `--acc2` kenarlık.

**Footer / iletişim**: mono bölüm numarası, maskeden yükselen başlık, mono büyük
e-posta bağlantısı (alt çizgi `--acc` %50, hover `--acc-hi`), altta hairline üstünde
`© <yıl> <ad> — <not>` ve GitHub / Google Play bağlantıları.

**Scroll ilerleme çubuğu**: en üstte 2px, `linear-gradient(90deg, var(--acc2), var(--acc))`,
genişlik = sayfa scroll yüzdesi.

### 2. Admin panel (`live-html/admin.html`)
Amaç: ana sayfadaki **her metin, liste ve bağlantıyı** kod düzenlemeden yönetmek.

Yerleşim: `grid-template-rows: auto 1fr`; sabit üst bar + (`>900px`) `236px 1fr` iki kolon,
altında tek kolona düşer. Sol kolon `sticky; top: 74px`.

**Üst bar**: badge + "Portfolyo yönetimi" + durum satırı
("kaydedilmemiş değişiklik var" / "kayıtlı — tarayıcı belleğinde"); sağda
`Siteyi gör ↗`, `JSON yükle` (gizli `input[type=file]` saran label), `JSON indir`,
`Sıfırla` (kırmızı outline, `confirm` sorar), `Kaydet` (dolu `--acc` pill).

**Sol menü** (8 bölüm, aktif olan `oklch(0.95 0.008 268 / 0.08)` zeminli, sağda kayıt sayısı):
`Genel & terminal`, `Banner`, `Hakkımda`, `Teknolojiler`, `Oyunlar`, `Demolar`, `GitHub`, `Menü & etiketler`.

**Sağ içerik**: bölüm başlığı + açıklama; dile bağlı bölümlerde sağ üstte
TR/EN/RU sekmeleri — her sekme o dilde **boş alan sayısını** gösterir (`EN · 3`, turuncu),
yanında `Boşları TR'den doldur` düğmesi (yalnız TR dışı dillerde; boş alanları TR metniyle kopyalar).

Alan tipleri: tek satır `input`, çok satır `textarea` (tam genişlik), `select` (kapak rengi).
Liste bölümlerinde her kayıt bir kart: mono sıra numarası + başlık, sağda `↑ ↓` sıralama,
(demolarda) `önizle`, ve `✕` sil. Kart altında alanlar `minmax(240px, 1fr)` ızgarada.
Sonda kesikli `+ … ekle` düğmesi.

**Demo önizleme**: `önizle` tam ekran overlay açar, `iframe srcdoc` ile yapıştırılan
HTML/JS'i çalıştırır (boşsa yer tutucu stub). Kaydetmeden test edilebilir.

**Toast**: alt ortada `--acc` dolgulu pill, ~2.2s görünür ("Kaydedildi", "JSON indirildi" …).

---

## Interactions & Behavior

| Etkileşim | Davranış |
|---|---|
| Scroll reveal | `IntersectionObserver`, `threshold 0.12`, `rootMargin 0 0 -5% 0`; `opacity 0 → 1`, `translateY(20–30px) → 0`; kapsayıcı içinde `0.07s` kademeli gecikme; bir kez tetiklenir (`unobserve`) |
| Maskeli başlık | `overflow: hidden` sarmalayıcıda `translateY(110%) → 0`; hero'da mount'ta, diğer bölümlerde görünürlükte |
| Parallax | `requestAnimationFrame` içinde `data-par` katsayısı × `110px` × yoğunluk; viewport dışı öğeler atlanır; tek rAF döngüsü, düğüm listesi önbelleklenir |
| Typewriter | Rol listesi döngüsü; yaz 62ms, sil 34ms, dolu bekleme 1500ms, tur arası 240ms; DOM'a doğrudan yazılır (re-render yok) |
| Terminal logları | Mount'tan 420ms sonra, satır başına 240ms |
| Sayaçlar | Görünürlükte `0 → değer`, toplam ~700ms |
| Dil değişimi | Anında; `localStorage["st-lang"]` |
| Demo satırı hover | Yapışkan mockup'ın adres çubuğu + başlığı güncellenir |
| Demo aç | Tam ekran `iframe srcdoc`; `Escape` veya "Kapat" ile çıkış |
| Admin → site senkronu | Site, `focus` ve `storage` olaylarında içeriği yeniden okur (admin başka sekmede kaydedince site güncellenir) |
| Responsive | 1000px: hero/demolar tek kolona, menü bağlantıları gizlenir; 900px: hakkımda ve admin kolonları tek kolona |
| Erişilebilirlik notu | Yeniden üretimde `prefers-reduced-motion: reduce` altında parallax/akış/typewriter kapatılmalı (prototipte yok) |

## State Management

Ana sayfa: `lang` (`tr|en|ru`, localStorage), `active` (aktif demo indexi), `demo`
(açık tam ekran demo indexi veya `null`), `data` (içerik).

Admin: `data`, `sec` (aktif bölüm), `lang` (düzenlenen dil), `dirty` (kaydedilmemiş),
`toast`, `preview` (önizlenen demo indexi).

Veri erişimi tek yerden: `site-data.js`
- `SEED` — başlangıç içeriği (mevcut gerçek metinler; production'da JSON'a taşınır)
- `loadContent()` — `localStorage["st-content-v1"]` üzerine `SEED` birleştirilir (eksik alanlar SEED'den)
- `saveContent(data)`, `resetContent()`, `downloadJSON(data, filename)`
- `COVERS` — kapak degrade preset'leri, `logColor(line)` — terminal satır rengi, `demoStub(title)` — boş demo yer tutucusu

**Production'da yapılacak**: `localStorage` yerine gerçek kalıcılık
(JSON dosyası + git, ya da SQLite/Postgres + admin auth). Şema aynı kalabilir.

## Content Schema

`site-data.js → SEED` yapısı (tam örnek veri o dosyada):

```ts
type Loc = { tr: string; en: string; ru: string };   // eksik dil → tr'ye düşer

type Content = {
  meta: { name; handle; mail; github; githubUser; play };          // hepsi string
  i18n: { tr: Copy; en: Copy; ru: Copy };
  logs: string[];                                                   // terminal satırları
  facts: { value: number; label: Loc }[];                           // sayaç kutuları
  techs: { name: string; note: string }[];
  games: { name: string; url: string; image: string; cover: keyof COVERS; desc: Loc }[]; // image: URL veya data URL; boşsa cover degradesi
  demos: { path: string; name: string; tags: string[]; html: string; desc: Loc }[];
  repos: { name: string; lang: string; updated: string; url: string; desc: Loc }[];
};

// Copy anahtarları (hepsi string, roles hariç):
// navAbout navStack navGames navDemos navGithub
// heroTag heroLine2 roles(string[]) heroLead heroBtn1 heroBtn2 scroll marquee
// aboutTitle aboutBig aboutSmall
// stackTitle stackLead stackSlot gamesTitle
// demosTitle demosLead demosHint demosSlot openDemo close
// githubTitle githubLead contactTitle contactHead footerNote imgSlot
```

Dile bağlı **olmayan** alanlar: `meta`, teknoloji adları, oyun/demo/depo adları,
`logs`, `tags`, `cover`, `url`, `updated`.

`demos[].html` tek dosya HTML/JS metnidir; `iframe srcdoc` ile çalıştırılır.
Production'da: kullanıcı içeriği olduğu için `sandbox="allow-scripts"` (aynı origin izni vermeden)
ve/veya demoları ayrı bir origin/subdomain'den servis et; admin'i auth arkasına al.

## Assets
Prototipte gerçek görsel yok. İhtiyaç duyulanlar:
- Google Fonts: Space Grotesk, JetBrains Mono (production'da self-host önerilir).
- Oyun kapakları: `games[].image` (16/10, `object-fit: cover`). Admin panelde URL yazılabilir veya dosya seçilebilir
  (prototipte data URL olarak saklanır — production'da dosya yükleme + CDN/статik klasör olmalı).
  Alan boşsa `cover` degrade preset'i ve mono yer tutucu etiketi gösterilir.
- İkon yok; tüm işaretler tipografik (`↗ ↑ ↓ ✕ → ✓ $`). İkon seti eklenecekse tek çizgi kalınlığında ince set uygun.

## Suggested implementation order
1. Tokenlar + tipografi + arka plan katmanı (statik) → boş sayfa doğru "hissettiğinde" devam.
2. Ana sayfa bölümleri statik içerikle, hifi ölçülerle.
3. İçerik katmanı (`Content` tipi + JSON) ve 3 dil (i18n fallback → `tr`).
4. Hareket katmanı: reveal, maskeli başlıklar, parallax, typewriter, terminal, sayaçlar (+ reduced-motion).
5. Demo çalıştırma (iframe + güvenlik) ve tam ekran overlay.
6. Admin CRUD + kalıcılık + auth; JSON içe/dışa aktarma korunsun (yedek olarak değerli).

## Files
- `live-html/index.html`, `live-html/admin.html` — bağımsız çalışan tek dosya site ve panel (JS gömülü)
- `source-dc/Ana Sayfa.dc.html`, `source-dc/Admin.dc.html` — okunabilir kaynaklar (şablon + logic)
- `source-dc/site-data.js` — içerik modeli, seed data, store yardımcıları
- `source-dc/support.js` — prototip runtime (referans dışı; yeniden üretimde kullanılmaz)
