/* ============================================================
   motion.js — HAREKET KATMANI (Faz 2)
   Kaynak: handoff README §Interactions & Behavior — süreler BİREBİR.

   Kapsam: scroll reveal · maskeli başlık · parallax · typewriter ·
           terminal log akışı · sayaçlar.
   ⛔ prefers-reduced-motion: reduce → hiçbiri çalışmaz (erken çıkış).
   ============================================================ */
(function () {
  "use strict";

  if (window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;
  if (!document.documentElement.classList.contains("anim")) return;

  var EASE_GECIKME = 0.07;   // kapsayıcı içi kademeli gecikme (s)
  var RISE_GECIKME = 0.09;   // maskeli başlık perdesi (s)

  /* ---------------------------------------------------------
     1) Maskeli başlıklar — hero'da MOUNT'ta, diğerlerinde GÖRÜNÜRLÜKTE
     --------------------------------------------------------- */
  function riseAc(el, sira) {
    el.style.transitionDelay = (sira * RISE_GECIKME + 0.1).toFixed(2) + "s";
    el.classList.add("is-in");
  }

  var heroRise = document.querySelectorAll("#top [data-rise]");
  requestAnimationFrame(function () {
    heroRise.forEach(function (el, i) { riseAc(el, i); });
  });

  /* ---------------------------------------------------------
     2) Scroll reveal + hero dışı maskeli başlıklar
        threshold .12 · rootMargin 0 0 -5% 0 · bir kez (unobserve)
     --------------------------------------------------------- */
  var gozlemci = null;
  if ("IntersectionObserver" in window) {
    gozlemci = new IntersectionObserver(function (girisler, gzl) {
      girisler.forEach(function (giris) {
        if (!giris.isIntersecting) return;
        var el = giris.target;
        el.classList.add("is-in");
        gzl.unobserve(el);                       // bir kez tetiklenir
        if (el.hasAttribute("data-count")) sayacBaslat(el);
      });
    }, { threshold: 0.12, rootMargin: "0px 0px -5% 0px" });
  }

  // Kapsayıcı içinde kademeli gecikme: [data-stagger] altındaki her öğe i*0.07s
  document.querySelectorAll("[data-stagger]").forEach(function (kap) {
    kap.querySelectorAll("[data-reveal]").forEach(function (el, i) {
      el.style.transitionDelay = (i * EASE_GECIKME).toFixed(2) + "s";
    });
  });

  function gozle(el) {
    if (gozlemci) { gozlemci.observe(el); }
    else { el.classList.add("is-in"); }          // IO yoksa doğrudan göster
  }

  document.querySelectorAll("[data-reveal]").forEach(gozle);

  /* ⚠️ MASKELİ BAŞLIK — prototipteki HATA burada düzeltildi:
     [data-rise] öğesi translateY(110%) ile `overflow:hidden` maskesinin DIŞINA
     taşınıyor. IntersectionObserver ata kutuların kırpmasını hesaba kattığı için
     öğenin görünür alanı 0 kalıyor → gözlemci HİÇ tetiklenmiyor → hero dışındaki
     5 bölüm başlığı (teknolojiler/oyunlar/demolar/github/iletişim) sonsuza dek
     gizli kalıyordu. Ölçüldü (2026-07-27): rise ratio 0, mask ratio 1.
     ÇÖZÜM: çocuğu değil MASKEYİ gözle; maske görününce içindeki başlığı aç.
     README'nin tarifi zaten bu: "diğer bölümlerde görünürlükte". */
  var maskeGozlemci = gozlemci
    ? new IntersectionObserver(function (girisler, gzl) {
        girisler.forEach(function (giris) {
          if (!giris.isIntersecting) return;
          giris.target.querySelectorAll("[data-rise]").forEach(function (c, i) { riseAc(c, i); });
          gzl.unobserve(giris.target);
        });
      }, { threshold: 0.12, rootMargin: "0px 0px -5% 0px" })
    : null;

  document.querySelectorAll(".mask").forEach(function (maske) {
    if (maske.closest("#top")) return;            // hero'nunkiler mount'ta açıldı
    var cocuklar = maske.querySelectorAll("[data-rise]");
    if (!cocuklar.length) return;
    if (maskeGozlemci) maskeGozlemci.observe(maske);
    else cocuklar.forEach(function (c) { c.classList.add("is-in"); });   // IO yoksa doğrudan göster
  });

  /* ---------------------------------------------------------
     3) Sayaçlar — görünürlükte 0 → değer, toplam ~700ms
     --------------------------------------------------------- */
  function sayacBaslat(el) {
    var hedef = parseInt(el.getAttribute("data-count"), 10);
    if (isNaN(hedef)) return;
    var sure = 700, basla = null;
    function adim(t) {
      if (basla === null) basla = t;
      var o = Math.min((t - basla) / sure, 1);
      el.textContent = Math.round(hedef * o);
      if (o < 1) requestAnimationFrame(adim);
    }
    el.textContent = "0";
    requestAnimationFrame(adim);
  }

  /* ---------------------------------------------------------
     4) Terminal log akışı — mount + 420ms, satır başına 240ms
     --------------------------------------------------------- */
  var loglar = document.querySelectorAll("[data-log]");
  if (loglar.length) {
    setTimeout(function () {
      loglar.forEach(function (satir, i) {
        setTimeout(function () { satir.classList.add("is-in"); }, i * 240);
      });
    }, 420);
  }

  /* ---------------------------------------------------------
     5) Typewriter — yaz 62ms · sil 34ms · dolu bekleme 1500ms · tur arası 240ms
        DOM'a doğrudan yazılır (re-render yok).
     --------------------------------------------------------- */
  var yazi = document.querySelector("[data-typed]");
  if (yazi) {
    var roller = [];
    try { roller = JSON.parse(yazi.getAttribute("data-roles") || "[]"); } catch (e) { roller = []; }
    if (roller.length) {
      var ri = 0, ci = 0, siliyor = false;
      yazi.textContent = "";
      (function tik() {
        var rol = roller[ri];
        if (!siliyor) {
          ci++;
          yazi.textContent = rol.slice(0, ci);
          if (ci === rol.length) { siliyor = true; return setTimeout(tik, 1500); }
          return setTimeout(tik, 62);
        }
        ci--;
        yazi.textContent = rol.slice(0, ci);
        if (ci === 0) { siliyor = false; ri = (ri + 1) % roller.length; return setTimeout(tik, 240); }
        setTimeout(tik, 34);
      })();
    }
  }

  /* ---------------------------------------------------------
     6) Parallax — data-par katsayısı × 110px × yoğunluk
        TEK rAF döngüsü, düğüm listesi önbellekli, viewport dışı atlanır.
     --------------------------------------------------------- */
  var parOgeler = Array.prototype.slice.call(document.querySelectorAll("[data-par]"));
  if (parOgeler.length) {
    var YOGUNLUK = 1;
    var beklemede = false;

    function parGuncelle() {
      var vh = window.innerHeight;
      for (var i = 0; i < parOgeler.length; i++) {
        var el = parOgeler[i];
        var k = parseFloat(el.getAttribute("data-par")) || 0;
        var r = el.getBoundingClientRect();
        if (r.bottom < 0 || r.top > vh) continue;              // viewport dışı → atla
        var orta = r.top + r.height / 2;
        var oran = (orta - vh / 2) / vh;                        // -0.5 … 0.5
        el.style.transform = "translate3d(0," + (oran * 110 * k * YOGUNLUK).toFixed(2) + "px,0)";
      }
      beklemede = false;
    }

    window.addEventListener("scroll", function () {
      if (!beklemede) { beklemede = true; requestAnimationFrame(parGuncelle); }
    }, { passive: true });
    window.addEventListener("resize", parGuncelle, { passive: true });
    parGuncelle();
  }
})();
