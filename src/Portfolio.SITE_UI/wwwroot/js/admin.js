/* admin.js — yönetim paneli etkileşimleri (Faz 5.2)
   · kaydedilmemiş değişiklik takibi (+ sayfadan ayrılma uyarısı)
   · silmede onay
   · toast otomatik kapanma
   · demo önizleme (sandbox'lı iframe, kaydetmeden)
*/
(function () {
  "use strict";

  /* ---------- Toast ---------- */
  var toast = document.querySelector("[data-toast]");
  if (toast) setTimeout(function () { toast.classList.add("gitti"); }, 2200);

  /* ---------- Kaydedilmemiş değişiklik ---------- */
  var form = document.querySelector("[data-kirli-form]");
  var durum = document.querySelector("[data-kirli-durum]");
  var kirli = false;

  function kirliYap() {
    if (kirli) return;
    kirli = true;
    if (durum) { durum.textContent = "kaydedilmemiş değişiklik var"; durum.classList.add("kirli"); }
  }

  if (form) {
    form.addEventListener("input", kirliYap);
    form.addEventListener("change", kirliYap);
    // Gönderim (kaydet/ekle/sil/taşı) sırasında uyarı çıkmasın
    form.addEventListener("submit", function () { kirli = false; });
  }

  window.addEventListener("beforeunload", function (e) {
    if (!kirli) return;
    e.preventDefault();
    e.returnValue = "";     // tarayıcı kendi metnini gösterir
  });

  /* ---------- Silme onayı ---------- */
  document.querySelectorAll("[data-onay]").forEach(function (btn) {
    btn.addEventListener("click", function (e) {
      if (!window.confirm(btn.getAttribute("data-onay"))) e.preventDefault();
    });
  });

  /* ---------- Demo önizleme ----------
     Kaydedilmiş HTML sunucudan gelir; iframe sandbox="allow-scripts" ile açılır
     (allow-same-origin YOK). Alanda yazılı ama KAYDEDİLMEMİŞ metin varsa onu
     kullanır → "kaydetmeden test et" akışı. */
  var overlay = document.querySelector("[data-onizle-overlay]");
  var frame = document.querySelector("[data-onizle-frame]");
  var adEtiketi = document.querySelector("[data-onizle-ad]");

  function onizleAc(index) {
    if (!overlay || !frame) return;
    var kutu = document.querySelector('textarea[name="oge[' + index + '].html"]');
    var ad = document.querySelector('input[name="oge[' + index + '].name"]');
    var yol = document.querySelector('input[name="oge[' + index + '].path"]');

    if (adEtiketi) adEtiketi.textContent = (yol && yol.value) || (ad && ad.value) || ("demo " + index);

    var html = kutu ? kutu.value : "";
    if (html.trim()) {
      frame.removeAttribute("src");
      frame.srcdoc = html;                       // kaydedilmemiş içerik
    } else {
      frame.removeAttribute("srcdoc");
      frame.src = "/admin/icerik/demo-onizle/" + index;   // kayıtlı içerik ya da stub
    }
    overlay.hidden = false;
    document.body.style.overflow = "hidden";
  }

  function onizleKapat() {
    if (!overlay || !frame) return;
    overlay.hidden = true;
    frame.removeAttribute("src");
    frame.srcdoc = "";                           // demo arkada çalışmaya devam etmesin
    document.body.style.overflow = "";
  }

  document.querySelectorAll("[data-onizle]").forEach(function (b) {
    b.addEventListener("click", function () { onizleAc(b.getAttribute("data-onizle")); });
  });
  document.querySelectorAll("[data-onizle-kapat]").forEach(function (b) {
    b.addEventListener("click", onizleKapat);
  });
  document.addEventListener("keydown", function (e) {
    if (e.key === "Escape" && overlay && !overlay.hidden) onizleKapat();
  });
})();
