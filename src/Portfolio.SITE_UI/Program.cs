using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Portfolio.Repositories;
using Portfolio.Services;
using Portfolio.Services.Auth;
using Portfolio.SITE_UI;

// `--setup-auth` → web sunucusu açmadan kimlik üretir ve çıkar.
if (args.Contains("--setup-auth"))
    return AuthSetup.Run(args);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// --- İçerik katmanı (Faz 3) ---
// JSON depo: DB'siz geliştirme. Faz 4'te ortam bazlı SqlContentStore devreye girecek.
builder.Services.AddSingleton<IContentStore>(_ =>
    new JsonContentStore(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "seed-content.json")));
builder.Services.AddScoped<IContentService, ContentService>();

// --- Kimlik doğrulama (Faz 5.1) ---
// Sırlar env'den gelir: Auth__Username / Auth__PasswordHash / Auth__TotpSecret.
// Repoda DURMAZ (bkz. `--setup-auth`).
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection(AuthOptions.SectionName));
builder.Services.AddSingleton<IAdminAuthenticator, AdminAuthenticator>();
builder.Services.AddSingleton<ILoginThrottle, LoginThrottle>();

var authOpt = builder.Configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

// ⛔ GÜVENLİK KAPISI: admin İNTERNETE AÇIK. Production'da kimlik yapılandırılmamışsa
// uygulama BAŞLAMAZ — korumasız bir admin paneli yayına çıkamaz.
// (AdminPanel'de uyarı yeterliydi: o panel Tailscale arkasındaydı. Burada değil.)
if (!authOpt.Enabled && !builder.Environment.IsDevelopment())
{
    throw new InvalidOperationException(
        "Auth yapılandırılmamış (Auth__PasswordHash / Auth__TotpSecret boş). " +
        "Admin paneli public olduğu için kimlik doğrulaması olmadan başlatılamaz. " +
        "Çözüm: `dotnet Portfolio.SITE_UI.dll --setup-auth` ile üret, systemd'de EnvironmentFile olarak bağla.");
}

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/admin/giris";
        o.LogoutPath = "/admin/cikis";
        o.AccessDeniedPath = "/admin/giris";
        o.ExpireTimeSpan = TimeSpan.FromHours(authOpt.SessionHours);
        o.SlidingExpiration = true;

        // `__Host-` öneki: tarayıcı bu çerezi yalnız Secure + Path=/ + Domain'siz kabul eder
        // → alt alan adına (demo.sinan73k1n.space) ASLA gönderilmez. Demo izolasyon
        // kararının çerez ayağı bu. Dev'de HTTP olduğu için önek kullanılamaz.
        var https = !builder.Environment.IsDevelopment();
        o.Cookie.Name = https ? "__Host-portfolio.admin" : "portfolio.admin";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Strict;
        o.Cookie.SecurePolicy = https ? CookieSecurePolicy.Always : CookieSecurePolicy.SameAsRequest;
        o.Cookie.Path = "/";
    });

builder.Services.AddAuthorization(o =>
{
    // Auth kapalıysa (yalnız Development'ta mümkün) admin login'siz gezilebilir.
    // Açıksa kimlik zorunlu.
    o.FallbackPolicy = null;
    o.AddPolicy("AdminPolicy", p =>
    {
        if (authOpt.Enabled) p.RequireAuthenticatedUser();
        else p.RequireAssertion(_ => true);
    });
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "admin",
    pattern: "admin/{controller=Dashboard}/{action=Index}/{id?}",
    defaults: new { area = "Admin" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Geliştirme kolaylığı: auth kapalıysa konsolda görünür uyarı.
if (!authOpt.Enabled)
    app.Logger.LogWarning("⚠️  Auth KAPALI (Auth__PasswordHash/TotpSecret yok) — /admin login'siz açılıyor. Yalnız geliştirme için.");

app.Run();
return 0;
