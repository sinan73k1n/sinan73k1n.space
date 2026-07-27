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

    [HttpGet("/d/{index:int}")]
    public async Task<IActionResult> Goster(int index, CancellationToken ct)
    {
        var html = await _icerik.DemoHtmlAsync(index, ct);
        if (string.IsNullOrEmpty(html)) return NotFound();

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
