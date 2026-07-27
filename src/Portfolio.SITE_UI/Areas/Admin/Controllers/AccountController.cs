using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Services.Auth;

namespace Portfolio.SITE_UI.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin")]
public class AccountController : Controller
{
    private readonly IAdminAuthenticator _kimlik;
    private readonly ILoginThrottle _kilit;
    private readonly ILogger<AccountController> _log;

    public AccountController(IAdminAuthenticator kimlik, ILoginThrottle kilit, ILogger<AccountController> log)
    {
        _kimlik = kimlik; _kilit = kilit; _log = log;
    }

    [HttpGet("giris")]
    public IActionResult Login(string? donus)
    {
        if (User.Identity?.IsAuthenticated == true) return YerelDonus(donus);
        ViewData["Donus"] = donus;
        return View();
    }

    [HttpPost("giris")]
    [ValidateAntiForgeryToken]                       // CSRF zorunlu
    public async Task<IActionResult> Login(string? kullanici, string? parola, string? kod, string? donus)
    {
        var ip = IstemciIp();

        // 1) Kilit kontrolü — kimlik doğrulamadan ÖNCE (PBKDF2 maliyeti de harcanmasın)
        var kalan = _kilit.KilitliMi(ip);
        if (kalan is not null)
        {
            _log.LogWarning("Admin giriş kilidi aktif: {Ip}, kalan {Saniye}sn", ip, (int)kalan.Value.TotalSeconds);
            ModelState.AddModelError("", $"Çok fazla başarısız deneme. {(int)kalan.Value.TotalSeconds} saniye sonra tekrar deneyin.");
            ViewData["Donus"] = donus;
            return View();
        }

        // 2) Auth kapalı (yalnız Development) → login ekranı zaten anlamsız
        if (!_kimlik.Enabled)
        {
            ModelState.AddModelError("", "Kimlik doğrulama yapılandırılmamış (geliştirme modu).");
            return View();
        }

        // 3) Doğrulama — şifre VE TOTP
        if (!_kimlik.Validate(kullanici, parola, kod))
        {
            _kilit.BasarisizDeneme(ip);
            _log.LogWarning("Admin giriş BAŞARISIZ: {Ip}", ip);
            // Hangi alanın yanlış olduğu SÖYLENMEZ (kullanıcı adı keşfi / TOTP ayrımı sızmasın)
            ModelState.AddModelError("", "Kullanıcı adı, şifre veya doğrulama kodu hatalı.");
            ViewData["Donus"] = donus;
            return View();
        }

        _kilit.Sifirla(ip);

        var kimlikBilgisi = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, _kimlik.Username),
            new Claim("admin", "true")
        }, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(kimlikBilgisi),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(_kimlik.SessionHours)
            });

        _log.LogInformation("Admin giriş BAŞARILI: {Kullanici} ({Ip})", _kimlik.Username, ip);
        return YerelDonus(donus);
    }

    [HttpPost("cikis")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    /// <summary>Açık yönlendirme (open redirect) engeli: yalnız site içi yollara dönülür.</summary>
    private IActionResult YerelDonus(string? donus) =>
        !string.IsNullOrWhiteSpace(donus) && Url.IsLocalUrl(donus)
            ? Redirect(donus)
            : RedirectToAction("Index", "Dashboard", new { area = "Admin" });

    /// <summary>
    /// Gerçek istemci IP'si. Sunucuda Cloudflare + Nginx arkasında olacağı için
    /// `CF-Connecting-IP` başlığı önceliklidir (Cloudflare'in garantili başlığı).
    /// </summary>
    private string IstemciIp()
    {
        var cf = Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cf)) return cf;

        var xreal = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(xreal)) return xreal;

        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "bilinmeyen";
    }
}
