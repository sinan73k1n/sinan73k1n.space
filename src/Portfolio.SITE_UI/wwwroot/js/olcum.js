/*
  olcum.js — sayfa içi ölçüm.

  NE ÖLÇER: hangi bölüme kadar inildi · her bölümde ne kadar kalındı ·
            hangi demo açıldı · hangi dış bağlantıya tıklandı.

  NE ÖLÇMEZ: fare hareketi, tuş vuruşu, kaydırma yolu, ekran görüntüsü.
             Ziyaretçi kimliği sunucuda üretiliyor ve her gece değişiyor;
             burada hiçbir kimlik oluşturulmuyor, çerez yazılmıyor.

  TASARIM:
  · Olaylar TAMPONLANIR, tek tek gönderilmez — bölüm bölüm kaydıran biri
    onlarca istek üretirdi. Sayfadan ayrılırken tek `sendBeacon` ile gider.
  · `sendBeacon` seçildi çünkü sayfa kapanırken de teslim edilir; normal
    fetch bu anda tarayıcı tarafından iptal edilir.
  · Her bölüm bir kez "görüldü" sayılır (tekrar tekrar aşağı-yukarı kaydırmak
    sayıyı şişirmesin).
  · Sekme arka plandayken süre sayacı DURUR — sekmeyi açık unutan biri
    "3 saat GitHub bölümünde kaldı" diye görünmesin.
*/
(function () {
    "use strict";

    var UC = "/olcum";
    if (!navigator.sendBeacon) return;   // eski tarayıcı: sessizce ölçüm yapma

    var kuyruk = [];
    var gorulen = {};          // bölüm → true (bir kez sayılır)
    var aktifBolum = null;
    var aktifBaslangic = 0;
    var sureler = {};          // bölüm → toplam ms

    function ekle(tip, deger, sure) {
        kuyruk.push({ tip: tip, deger: deger, sure: sure || 0 });
        // Uç tek istekte 20 olay kabul ediyor; sınıra dayanınca erken gönder.
        if (kuyruk.length >= 15) gonder();
    }

    function gonder() {
        sureleriKapat();
        if (!kuyruk.length) return;

        var veri = kuyruk.splice(0, kuyruk.length);
        try {
            navigator.sendBeacon(UC, new Blob([JSON.stringify(veri)], { type: "application/json" }));
        } catch (e) { /* ölçüm başarısızlığı sayfayı etkilemez */ }
    }

    // --- Süre sayacı ---------------------------------------------------------

    function sureBaslat(bolum) {
        sureDurdur();
        aktifBolum = bolum;
        aktifBaslangic = Date.now();
    }

    function sureDurdur() {
        if (!aktifBolum) return;
        var gecen = Date.now() - aktifBaslangic;
        if (gecen > 0) sureler[aktifBolum] = (sureler[aktifBolum] || 0) + gecen;
        aktifBolum = null;
    }

    /** Biriken süreleri olaya çevirir. 2 saniyenin altı GÜRÜLTÜ sayılır ve atılır. */
    function sureleriKapat() {
        sureDurdur();
        for (var bolum in sureler) {
            if (!Object.prototype.hasOwnProperty.call(sureler, bolum)) continue;
            var saniye = Math.round(sureler[bolum] / 1000);
            if (saniye >= 2) kuyruk.push({ tip: "sure", deger: bolum, sure: saniye });
        }
        sureler = {};
    }

    // --- Bölüm görünürlüğü ---------------------------------------------------

    var bolumler = document.querySelectorAll("[data-bolum]");
    if (bolumler.length && "IntersectionObserver" in window) {
        var gozlemci = new IntersectionObserver(function (girdiler) {
            girdiler.forEach(function (g) {
                var ad = g.target.getAttribute("data-bolum");
                if (!ad) return;

                if (g.isIntersecting) {
                    if (!gorulen[ad]) { gorulen[ad] = true; ekle("bolum", ad); }
                    sureBaslat(ad);
                } else if (aktifBolum === ad) {
                    sureDurdur();
                }
            });
        }, {
            // Bölümün yarısı ekrana girmeden "görüldü" saymıyoruz: hızlı kaydırırken
            // ekranın kenarından geçen her bölüm görülmüş sayılsaydı "nereye kadar
            // indi" ölçüsü anlamını yitirirdi.
            threshold: 0.5
        });

        Array.prototype.forEach.call(bolumler, function (b) { gozlemci.observe(b); });
    }

    // --- Demo açılışı --------------------------------------------------------

    document.addEventListener("click", function (e) {
        // Demo hem satıra tıklanarak hem "çalıştır" düğmesiyle açılabiliyor.
        // Hangi demonun açıldığını demo.js ile AYNI kaynaktan okuyoruz — aktif satır.
        var demoSatir = e.target.closest ? e.target.closest("[data-demo]") : null;
        var demoBtn = e.target.closest ? e.target.closest("[data-demo-open]") : null;
        if (demoSatir || demoBtn) {
            var kaynak = demoSatir || document.querySelector("[data-demo].is-active");
            var ad = kaynak ? kaynak.getAttribute("data-demo-ad") : null;
            if (ad) ekle("demo", ad);
            return;
        }

        // --- Dış bağlantılar ---
        var a = e.target.closest ? e.target.closest("a[href]") : null;
        if (!a) return;

        var href = a.getAttribute("href") || "";
        if (href.indexOf("mailto:") === 0) { ekle("baglanti", "e-posta"); return; }

        // Yalnız BAŞKA bir siteye giden bağlantılar; sayfa içi çapalar değil.
        if (!/^https?:\/\//i.test(href)) return;
        try {
            var u = new URL(href);
            if (u.host === location.host) return;
            ekle("baglanti", u.host + u.pathname.replace(/\/$/, ""));
        } catch (err) { /* bozuk URL: yok say */ }
    }, true);

    // --- Gönderim anları -----------------------------------------------------

    // Sekme arka plana geçtiğinde hem süreyi durdur hem biriken olayları yolla:
    // mobilde `beforeunload` çoğu zaman hiç çalışmaz, güvenilir olan budur.
    document.addEventListener("visibilitychange", function () {
        if (document.visibilityState === "hidden") gonder();
        else if (aktifBolum === null) { /* geri dönünce gözlemci yeniden başlatır */ }
    });

    window.addEventListener("pagehide", gonder);
})();
