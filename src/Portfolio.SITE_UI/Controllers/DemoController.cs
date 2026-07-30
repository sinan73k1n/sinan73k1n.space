using Microsoft.AspNetCore.Mvc;
using Portfolio.Services.Admin;

namespace Portfolio.SITE_UI.Controllers;

/// <summary>
/// Demoları HAM HTML olarak servis eder — yalnız iframe içinde kullanılmak üzere.
/// Ayrı origin'den (demo.sinan73k1n.space) yayınlanır; ana site oraya iframe açar.
/// </summary>
public class DemoController : Controller
{
    private readonly IAdminContentService _icerik;

    public DemoController(IAdminContentService icerik) => _icerik = icerik;

    /// <summary>
    /// Küçük önizleme olarak gömülen demoya eklenen çağrı. Demo bu fonksiyonu
    /// tanımlarsa (örn. giriş ekranını atlamak için) haberdar olur; tanımlamayan
    /// demo için hiçbir şey değişmez. Kullanıcının HTML'i DEĞİŞTİRİLMEZ — yalnız
    /// sonuna eklenir ve demonun kendi izole belgesinde çalışır.
    /// </summary>
    private const string OnizlemeKancasi =
        "<script>try{window.__onizleme&&window.__onizleme()}catch(e){}</script>";

    /// <param name="onizleme">
    /// 1 ise demo, ana sayfadaki mockup içinde <b>tıklanamaz küçük kopya</b> olarak
    /// açılıyor demektir (bkz. <c>OnizlemeKancasi</c>). Tam ekranda verilmez.
    /// </param>
    [HttpGet("/d/{index:int}")]
    public async Task<IActionResult> Goster(int index, CancellationToken ct,
                                            [FromQuery(Name = "onizleme")] int onizleme = 0)
    {
        var html = await _icerik.DemoHtmlAsync(index, ct);
        if (string.IsNullOrEmpty(html)) return NotFound();
        if (onizleme == 1) html += OnizlemeKancasi;

        // Demo, ana sitenin iframe'i içinde açılır; başka sitelerin çerçevelemesi engellenir.
        Response.Headers["Content-Security-Policy"] =
            "frame-ancestors 'self' https://sinan73k1n.space https://www.sinan73k1n.space";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        // Bu içerik kullanıcı tarafından yapıştırılmıştır: arama motoruna girmesin.
        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";

        return Content(html, "text/html; charset=utf-8");
    }
}
