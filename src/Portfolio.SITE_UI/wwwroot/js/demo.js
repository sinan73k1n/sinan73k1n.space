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
  if (!overlay || !frame) return;

  var sonOdak = null;

  function veriOku(index) {
    var el = document.querySelector('[data-demo-src="' + index + '"]');
    if (!el) return null;
    try { return JSON.parse(el.textContent); } catch (e) { return null; }
  }

  function ac(index) {
    var veri = veriOku(index);
    if (!veri) return;

    sonOdak = document.activeElement;
    if (yolEtiketi) yolEtiketi.textContent = "sinantekin.dev/" + veri.path;

    frame.srcdoc = veri.html;
    overlay.hidden = false;
    document.body.style.overflow = "hidden";        // arka planda kaydırma kilitlensin

    var kapat = overlay.querySelector("[data-demo-close]");
    if (kapat) kapat.focus();
  }

  function kapat() {
    overlay.hidden = true;
    frame.srcdoc = "";                              // demo çalışmaya devam etmesin
    document.body.style.overflow = "";
    if (sonOdak && sonOdak.focus) sonOdak.focus();
  }

  // Demo satırına tıklama / Enter-Space (satır role="button" tabindex=0)
  document.querySelectorAll("[data-demo]").forEach(function (satir) {
    var index = satir.getAttribute("data-demo");
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
})();
