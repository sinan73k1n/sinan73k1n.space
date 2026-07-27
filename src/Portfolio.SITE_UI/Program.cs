using Portfolio.Repositories;
using Portfolio.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// --- İçerik katmanı (Faz 3) ---
// JSON depo: DB'siz geliştirme. Faz 4'te ortam bazlı SqlContentStore devreye girecek
// (Tailscale + MSSQL gerekiyor) — arayüz aynı kaldığı için view/servis değişmeyecek.
builder.Services.AddSingleton<IContentStore>(_ =>
    new JsonContentStore(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "seed-content.json")));
builder.Services.AddScoped<IContentService, ContentService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
