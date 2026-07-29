using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Portfolio.Repositories;
using Portfolio.Repositories.Sql;
using Portfolio.Services.Metrik;
using Portfolio.SITE_UI.Metrik;
using Portfolio.Services;
using Portfolio.Services.Auth;
using Portfolio.SITE_UI;

// `--setup-auth` → web sunucusu açmadan kimlik üretir ve çıkar.
if (args.Contains("--setup-auth"))
    return AuthSetup.Run(args);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// --- İçerik katmanı (Faz 3 JSON · Faz 4 MSSQL) ---
//
// ⚠️ DEPLOY GÜVENLİĞİ: canlı içerik dosyası publish klasörünün DIŞINDA durmalı.
// Aksi halde her rsync, admin'den yapılan düzenlemeleri repo tohumuyla ezer.
// Sunucuda:  Content__FilePath=$HOME/portfolio-data/content.json
// Dosya yoksa repodaki tohumdan bir kez kopyalanır (idempotent).
// Tohum artık bir DOSYA değil, KOD (`SeedIcerik`) — bkz. o sınıfın notu.
// Yol verilmezse geliştirme dosyası ContentRoot altında oluşur.
var icerikYolu = builder.Configuration["Content:FilePath"];
if (string.IsNullOrWhiteSpace(icerikYolu))
    icerikYolu = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "content.json");

// JSON depo her hâlükârda kurulur: SQL açıkken bile ilk tohumlama kaynağı odur
// (sunucudaki mevcut content.json → DB; o da yoksa koddaki seed).
var jsonDepo = new JsonContentStore(icerikYolu);

// Depo seçimi TEK ŞEYE bakar: bağlantı dizesi var mı?
//   var  → MSSQL (`portfoliodb`, sunucu)      · yok → JSON dosyası (Mac'te geliştirme)
// Dize REPODA DURMAZ; systemd EnvironmentFile'ından gelir:
//   ConnectionStrings__Portfolio=Server=...;Database=portfoliodb;User Id=portfolio_app;...
var sqlBaglanti = builder.Configuration.GetConnectionString("Portfolio");
var sqlAktif = !string.IsNullOrWhiteSpace(sqlBaglanti);

if (sqlAktif)
{
    builder.Services.AddDbContextFactory<PortfolioDbContext>(o => o.UseSqlServer(sqlBaglanti));
    builder.Services.AddSingleton<IContentStore>(sp =>
        new SqlContentStore(sp.GetRequiredService<IDbContextFactory<PortfolioDbContext>>()));

    // --- Metrikler (Faz 8) — yalnız DB varken ---
    // Ölçüm sitenin ÖN KOŞULU değil: DB yoksa (Mac'te geliştirme) hiç kaydedilmez,
    // site aynen çalışır. Bu yüzden hepsi bu bloğun içinde.
    builder.Services.AddSingleton<IMetrikDeposu, MetrikDeposu>();
    builder.Services.AddSingleton<IMetrikKuyrugu>(_ => new MetrikKuyrugu());
    builder.Services.AddSingleton<IMetrikOkuyucu, MetrikOkuyucu>();
    builder.Services.AddHostedService<MetrikYazici>();     // kuyruk → DB
    builder.Services.AddHostedService<MetrikOzetleyici>(); // gecelik özet + temizlik
}
else
{
    builder.Services.AddSingleton<IContentStore>(jsonDepo);
}

// --- Demo izolasyonu (Faz 5.7a) ---
// Ayarlıysa demolar AYRI ORIGIN'den servis edilir (demo.sinan73k1n.space):
// tarayıcının origin kuralı, sandbox'ın yanında ikinci bağımsız katman olur.
// Boşsa (geliştirme) srcdoc + sandbox ile çalışır.
builder.Services.AddSingleton(new DemoOrigin(builder.Configuration["Demo:Origin"]));
builder.Services.AddScoped<IContentService, ContentService>();
builder.Services.AddScoped<Portfolio.Services.Admin.IAdminContentService, Portfolio.Services.Admin.AdminContentService>();
// --- Yüklenen oyun kapakları (dosya sistemi; DB'ye binary konmaz) ---
//
// ⚠️ İKİ AYRI TUZAK VARDI, ikisi de burada kapanıyor:
//  1) Dosyalar `wwwroot/uploads` altına, yani PUBLISH KLASÖRÜNÜN İÇİNE yazılıyordu.
//     İçerik dosyasıyla aynı gerekçe: publish içinde duran veri deploy'da kaybolur.
//     Artık canlı içerik dosyasının YANINDA durur (`portfolio-data/uploads`).
//  2) Statik dosyalar `MapStaticAssets()` ile servis ediliyor; o, DERLEME ANINDA
//     üretilmiş bir manifestten okur → çalışma anında yüklenen dosya orada YOKTUR
//     ve 404 döner. Yükleme klasörü bu yüzden AYRICA `UseStaticFiles` ile bağlanır.
var medyaKok = builder.Configuration["Media:Root"];
if (string.IsNullOrWhiteSpace(medyaKok))
    medyaKok = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(icerikYolu))!, "uploads");
Directory.CreateDirectory(medyaKok);                    // yoksa dosya sağlayıcı açılışta patlar
builder.Services.AddSingleton<IMediaStore>(_ => new FileMediaStore(medyaKok));

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

// --- DB hazırlığı (Faz 4) ---
// Şema + tohumlama açılışta, TEK SEFER. İkisi de idempotent:
//   · Migrate  → uygulanmamış migration varsa uygular, yoksa dokunmaz.
//   · Tohumla  → DB boşsa canlı JSON içeriğini aktarır, DOLUYSA HİÇBİR ŞEY YAPMAZ.
// Böylece JSON'dan SQL'e geçiş elle bir aktarım adımı gerektirmiyor ve sonraki
// her deploy'da bu blok zararsızca no-op oluyor.
// ⚠️ KARAR: DB erişilemezse uygulama BAŞLAMAZ (sessizce JSON'a düşmez).
// Alternatifi denendi ve elendi: JSON'a düşmek, admin'den yapılan kayıtların
// "kaydedildi" görünüp kaybolmasına ve ziyaretçiye sessizce eski içerik
// sunulmasına yol açardı. Açık bir systemd hatası, sessiz yanlış içerikten iyi.
if (sqlAktif)
{
    await using var kapsam = app.Services.CreateAsyncScope();
    var fabrika = kapsam.ServiceProvider.GetRequiredService<IDbContextFactory<PortfolioDbContext>>();
    await using (var db = await fabrika.CreateDbContextAsync())
        await db.Database.MigrateAsync();

    var depo = (SqlContentStore)kapsam.ServiceProvider.GetRequiredService<IContentStore>();

    // JSON tohumu YALNIZ gerçekten gerekiyorsa okunur. Aksi hâlde DB'ye geçtikten
    // sonra o dosyanın silinmesi (artık kaynak değil, yalnız anlık görüntü)
    // uygulamayı başlatamaz hâle getirirdi.
    if (await depo.TohumlamaGerekliMiAsync())
    {
        await depo.SaveAsync(await jsonDepo.LoadAsync());
        app.Logger.LogInformation("İçerik JSON'dan portfoliodb'ye aktarıldı (ilk tohumlama).");
    }
    else
    {
        app.Logger.LogInformation("İçerik kaynağı: portfoliodb (tohumlama gerekmedi, DB zaten dolu).");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// ⚠️ UseHttpsRedirection YOK — TLS yukarıda sonlanıyor (Cloudflare "Always Use HTTPS"),
// Kestrel yalnız 127.0.0.1'de HTTP dinliyor. Uygulama içi yönlendirme hem gereksiz
// hem de "https portu belirlenemedi" uyarısı üretiyordu.
// Bunun yerine ters vekilin ilettiği şema/IP başlıklarını tanı:
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor
    // KnownProxies varsayılanı loopback → yalnız yerel Nginx'e güvenilir,
    // dışarıdan gelen sahte X-Forwarded-* başlıkları dikkate alınmaz.
});

// 404 vb. için kendi sayfamız (Faz 7). ReExecute: adres çubuğu değişmez,
// durum kodu 404 kalır — yönlendirme yapan varyantı SEO açısından yanlış olurdu.
app.UseStatusCodePagesWithReExecute("/Home/Bulunamadi/{0}");

// Yüklenen kapaklar: `/uploads/**` → `medyaKok`. MapStaticAssets bu dosyaları GÖREMEZ
// (derleme anı manifesti), bu yüzden klasik statik dosya ara katmanı ile bağlanıyor.
// Dosya adlarını biz üretiyoruz (tarih + guid) ve asla yeniden kullanmıyoruz →
// içerik değişmez, uzun önbellek güvenli.
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(medyaKok),
    RequestPath = "/uploads",
    // ⛔ Bilinmeyen uzantı SERVİS EDİLMEZ (varsayılan). Yükleyici zaten yalnız
    // görsel uzantısına izin veriyor; bu ikinci kapı.
    OnPrepareResponse = ctx =>
        ctx.Context.Response.Headers.CacheControl = "public,max-age=31536000,immutable"
});

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
