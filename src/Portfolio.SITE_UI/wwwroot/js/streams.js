/* streams.js — arka plandaki 16 dikey "veri hattı"nı üretir.
   Kaynak: prototip buildStreams() (design_handoff_portfolio).
   Kolon x konumları ve gecikmeler RASTGELE — düzenli görünmemeli.
   ⛔ prefers-reduced-motion: reduce → hiç üretilmez. */
(function () {
  "use strict";

  if (window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

  var host = document.querySelector("[data-streams]");
  if (!host || host.childElementCount) return;

  var GLYPHS = ["01", "{ }", "</>", "10", "fn", "::", "=>", "01"];
  var COUNT = 16;

  for (var i = 0; i < COUNT; i++) {
    var col = document.createElement("div");
    col.className = "bg-stream";
    // eşit aralık + ±1.5% rastgele kayma
    col.style.left = ((i / COUNT) * 100 + (Math.random() * 3 - 1.5)).toFixed(2) + "%";

    var glyph = document.createElement("div");
    var dur = 16 + Math.random() * 22;                 // 16–38s
    glyph.className = "bg-stream__glyph " + (i % 2 ? "bg-stream__glyph--b" : "bg-stream__glyph--a");
    glyph.style.animationDuration = dur.toFixed(1) + "s";
    glyph.style.animationDelay = (-Math.random() * dur).toFixed(1) + "s";  // negatif = akış başlamış görünür
    glyph.textContent = GLYPHS[i % GLYPHS.length];

    col.appendChild(glyph);
    host.appendChild(col);
  }
})();
