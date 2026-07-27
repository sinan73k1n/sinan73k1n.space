using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Services;
using Portfolio.Services.Auth;

namespace Portfolio.SITE_UI.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin")]
[Authorize(Policy = "AdminPolicy")]      // auth kapalıysa (yalnız dev) serbest, açıksa zorunlu
public class DashboardController : Controller
{
    private readonly IContentService _icerik;
    private readonly IAdminAuthenticator _kimlik;

    public DashboardController(IContentService icerik, IAdminAuthenticator kimlik)
    {
        _icerik = icerik; _kimlik = kimlik;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        // Faz 5.2+'da bölüm bölüm CRUD gelecek. Şu an: kimlik doğrulama iskeleti +
        // içerik özeti (kaç kayıt var, hangi dilde kaç boş alan).
        var icerik = await _icerik.GetAsync(Portfolio.Entities.Lang.Tr, ct);
        ViewData["AuthAcik"] = _kimlik.Enabled;
        return View(icerik);
    }
}
