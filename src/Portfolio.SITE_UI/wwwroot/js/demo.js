/* demo.js — tam ekran demo çalıştırma (Faz 3.5)
 *
 * ⛔ GÜVENLİK (vault workflow-rules §3):
 *    · iframe `sandbox="allow-scripts"` — **allow-same-origin ASLA**.
 *      İkisi birlikte verilirse sandbox anlamsızlaşır: demo JS'i sitenin
 *      cookie'lerine, localStorage'ına ve DOM'una erişebilir hâle gelir.
 *    · Demo HTML'i sayfaya gömülmez; <script type="application/json"> içinden
 *      JSON olarak okunur (kapanış etiketi tuzağı oluşmaz).
 *    · srcdoc kullanılıyor → sandbox nedeniyle iframe OPAQUE origin alır.
 */
(function () {
  "use strict";

  var overlay = document.querySelector("[data-demo-overlay]");
  var frame = document.querySelector("[data-demo-frame]");
  var yolEtiketi = document.querySelector("[data-overlay-path]");
  var onizleme = document.querySelector("[data-demo-preview]");
  var iskelet = document.querySelector("[data-demo-skeleton]");
  if (!overlay || !frame) return;

  var sonOdak = null;

  /* Önizlemede çalışan demoya "küçük kopyasın" diye haber veren kanca.
     Demo isterse `window.__onizleme` tanımlar (örn. giriş ekranını atlar);
     tanımlamayan demo için hiçbir şey değişmez. Kullanıcı HTML'i DEĞİŞTİRİLMEZ,
     yalnız sonuna bu çağrı eklenir — ve o da demonun kendi sandbox'ında çalışır. */
  var ONIZLEME_KANCASI =
    "<script>try{window.__onizleme&&window.__onizleme()}catch(e){}<\/script>";

  function veriOku(index) {
    var el = document.querySelector('[data-demo-src="' + index + '"]');
    if (!el) return null;
    try { return JSON.parse(el.textContent); } catch (e) { return null; }
  }

  /* Mockup'ın içini seçili demonun küçük, tıklanamaz kopyasıyla doldurur.
     Tam ekran akışıyla AYNI kaynak seçimi: ayrı origin varsa oradan (tarayıcının
     origin duvarı), yoksa srcdoc + sandbox. Fark yalnız `?onizleme=1` — sunucu
     kancayı o zaman ekliyor (bkz. DemoController). */
  function onizlemeYaz(index) {
    if (!onizleme) return;
    var veri = veriOku(index);
    var html = veri && veri.html ? veri.html : "";
    if (!html) {                                  // HTML girilmemiş kayıt → iskelet
      onizleme.hidden = true;
      onizleme.removeAttribute("src");
      onizleme.removeAttribute("srcdoc");
      if (iskelet) iskelet.hidden = false;
      return;
    }
    if (iskelet) iskelet.hidden = true;
    onizleme.hidden = false;

    var ayriOrigin = frame.getAttribute("data-demo-origin");
    if (ayriOrigin) {
      onizleme.removeAttribute("srcdoc");
      var yeni = ayriOrigin + "/d/" + index + "?onizleme=1";
      if (onizleme.getAttribute("src") !== yeni) onizleme.src = yeni;   // aynı demo yeniden yüklenmesin
    } else {
      onizleme.removeAttribute("src");
      onizleme.srcdoc = html + ONIZLEME_KANCASI;
    }
  }

  function ac(index) {
    var veri = veriOku(index);
    if (!veri) return;

    sonOdak = document.activeElement;
    if (yolEtiketi) yolEtiketi.textContent = "sinantekin.dev/" + veri.path;

    // Ayrı origin kuruluysa demoyu ORADAN yükle (tarayıcının origin duvarı devreye girer);
    // yoksa srcdoc + sandbox ile aynı origin'de ama izole çalıştır.
    var ayriOrigin = frame.getAttribute("data-demo-origin");
    if (ayriOrigin) {
      frame.removeAttribute("srcdoc");
      frame.src = ayriOrigin + "/d/" + index;
    } else {
      frame.removeAttribute("src");
      frame.srcdoc = veri.html;
    }
    overlay.hidden = false;
    document.body.style.overflow = "hidden";        // arka planda kaydırma kilitlensin

    var kapat = overlay.querySelector("[data-demo-close]");
    if (kapat) kapat.focus();
  }

  function kapat() {
    overlay.hidden = true;
    frame.removeAttribute("src");
    frame.srcdoc = "";                              // demo çalışmaya devam etmesin
    document.body.style.overflow = "";
    if (sonOdak && sonOdak.focus) sonOdak.focus();
  }

  // Demo satırına tıklama / Enter-Space (satır role="button" tabindex=0)
  document.querySelectorAll("[data-demo]").forEach(function (satir) {
    var index = satir.getAttribute("data-demo");
    // Üstüne gelince mockup o demoyu gösterir (ui.js yolu/adı yazıyor, biz içeriği).
    satir.addEventListener("mouseenter", function () { onizlemeYaz(index); });
    satir.addEventListener("focus", function () { onizlemeYaz(index); });
    satir.addEventListener("click", function () { ac(index); });
    satir.addEventListener("keydown", function (e) {
      if (e.key === "Enter" || e.key === " ") { e.preventDefault(); ac(index); }
    });
  });

  // Mockup'taki "Tam ekran aç" — o an aktif olan demoyu açar
  document.querySelectorAll("[data-demo-open]").forEach(function (btn) {
    btn.addEventListener("click", function () {
      var aktif = document.querySelector("[data-demo].is-active");
      ac(aktif ? aktif.getAttribute("data-demo") : btn.getAttribute("data-demo-open"));
    });
  });

  overlay.querySelectorAll("[data-demo-close]").forEach(function (b) {
    b.addEventListener("click", kapat);
  });

  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape" && !overlay.hidden) kapat();
  });

  // Açılışta aktif satırın (ilk demo) önizlemesi. Sayfa yüklenirken hemen
  // başlatmıyoruz: 60 KB'lık srcdoc ilk boyamayı geciktirebilir, hero animasyonu
  // öne alınsın. Görünür alana girince yükleriz.
  var ilk = document.querySelector("[data-demo].is-active") || document.querySelector("[data-demo]");
  if (ilk && onizleme) {
    var yukle = function () { onizlemeYaz(ilk.getAttribute("data-demo")); };
    if ("IntersectionObserver" in window) {
      var izleyici = new IntersectionObserver(function (girisler) {
        if (girisler.some(function (g) { return g.isIntersecting; })) {
          yukle();
          izleyici.disconnect();
        }
      }, { rootMargin: "300px" });
      // ⚠️ iframe'in KENDİSİ izlenmez: başlangıçta `hidden` olduğu için layout kutusu
      // yok ve IntersectionObserver onu ASLA "göründü" saymaz (önizleme hiç yüklenmezdi).
      // Kapsayıcı (mockup ekranı) izlenir.
      izleyici.observe(onizleme.parentElement || onizleme);
    } else {
      yukle();
    }
  }
})();
