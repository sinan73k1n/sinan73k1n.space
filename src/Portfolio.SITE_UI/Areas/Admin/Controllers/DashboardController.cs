using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Repositories;
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
    private readonly IContentStore _depo;

    public DashboardController(IContentService icerik, IAdminAuthenticator kimlik, IContentStore depo)
    {
        _icerik = icerik; _kimlik = kimlik; _depo = depo;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var icerik = await _icerik.GetAsync(Portfolio.Entities.Lang.Tr, ct);
        ViewData["AuthAcik"] = _kimlik.Enabled;

        // Hangi depo devrede? Bu, "düzenlediğim şey nereye yazılıyor" sorusunun cevabı —
        // sabit metin yazmak yerine gerçek türe bakıyoruz ki yanlış bilgi vermesin.
        ViewData["IcerikKaynagi"] = _depo switch
        {
            Portfolio.Repositories.Sql.SqlContentStore => "MSSQL · portfoliodb",
            JsonContentStore => "JSON dosyası (DB bağlantı dizesi yok — geliştirme)",
            _ => _depo.GetType().Name
        };

        return View(icerik);
    }
}
