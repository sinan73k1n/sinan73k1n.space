/* ui.js — Faz 1.5 etkileşimleri.
   Şu an yalnız scroll ilerleme çubuğu + demo satırı hover senkronu (mockup adres/başlık).
   Reveal / typewriter / sayaç / parallax → Faz 2 (ayrı dosya: motion.js). */
(function () {
  "use strict";

  /* --- Scroll ilerleme çubuğu (en üstte 2px) --- */
  var bar = document.querySelector("[data-progress]");
  if (bar) {
    var beklemede = false;
    var guncelle = function () {
      var h = document.documentElement.scrollHeight - window.innerHeight;
      bar.style.width = (h > 0 ? (window.scrollY / h) * 100 : 0).toFixed(2) + "%";
      beklemede = false;
    };
    // tek rAF döngüsü: scroll olayında doğrudan yazma (jank yapar)
    window.addEventListener("scroll", function () {
      if (!beklemede) { beklemede = true; requestAnimationFrame(guncelle); }
    }, { passive: true });
    guncelle();
  }

  /* --- Demo satırı hover → yapışkan mockup güncellenir --- */
  var satirlar = document.querySelectorAll("[data-demo]");
  var url = document.querySelector("[data-demo-url]");
  var isim = document.querySelector("[data-demo-name]");
  if (satirlar.length && url && isim) {
    satirlar.forEach(function (satir) {
      satir.addEventListener("mouseenter", function () {
        satirlar.forEach(function (s) { s.classList.remove("is-active"); });
        satir.classList.add("is-active");
        var yol = satir.querySelector(".demo__path");
        var ad = satir.querySelector(".demo__name");
        if (yol) url.textContent = "sinantekin.dev/" + yol.textContent.trim();
        if (ad) isim.textContent = ad.textContent.trim();
      });
    });
  }
})();
