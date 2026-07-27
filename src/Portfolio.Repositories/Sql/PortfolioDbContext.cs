using Microsoft.EntityFrameworkCore;
using Portfolio.Entities.Content;

namespace Portfolio.Repositories.Sql;

/// <summary>
/// `portfoliodb` — portfolyonun kendi veritabanı (sunucudaki mssql container'ında,
/// CasinoBonusDb/admindb'den bağımsız). Yalnız içerik tabloları; kimlik/2FA sırları
/// DB'ye GİRMEZ (env dosyasında durur).
/// </summary>
public sealed class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<SiteMetaRow> SiteMeta => Set<SiteMetaRow>();
    public DbSet<CopyEntry> Copy => Set<CopyEntry>();
    public DbSet<HeroRoleRow> HeroRoles => Set<HeroRoleRow>();
    public DbSet<TerminalLogRow> TerminalLogs => Set<TerminalLogRow>();
    public DbSet<FactRow> Facts => Set<FactRow>();
    public DbSet<TechRow> Techs => Set<TechRow>();
    public DbSet<GameRow> Games => Set<GameRow>();
    public DbSet<DemoRow> Demos => Set<DemoRow>();
    public DbSet<RepoRow> Repos => Set<RepoRow>();
    public DbSet<CoverPresetRow> Covers => Set<CoverPresetRow>();

    // --- Metrikler (Faz 8) — içerikle aynı DB, ayrı tablolar ---
    public DbSet<ZiyaretRow> Ziyaretler => Set<ZiyaretRow>();
    public DbSet<OlayRow> Olaylar => Set<OlayRow>();
    public DbSet<GunlukOzetRow> GunlukOzetler => Set<GunlukOzetRow>();
    public DbSet<GunlukTuzRow> GunlukTuzlar => Set<GunlukTuzRow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Dil kodu 8 karakteri geçmez ("tr"/"en"/"ru"); indeks dar kalsın.
        const int DilUzunluk = 8;

        b.Entity<SiteMetaRow>(e =>
        {
            e.ToTable("SiteMeta");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).ValueGeneratedNever();   // tek satır, Id sabit 1
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Handle).HasMaxLength(200);
            e.Property(x => x.Mail).HasMaxLength(320);     // RFC 5321 üst sınırı
            e.Property(x => x.Github).HasMaxLength(500);
            e.Property(x => x.GithubUser).HasMaxLength(200);
            e.Property(x => x.Play).HasMaxLength(500);
        });

        b.Entity<CopyEntry>(e =>
        {
            e.ToTable("CopyEntry");
            e.Property(x => x.Key).HasMaxLength(100);
            e.Property(x => x.Lang).HasMaxLength(DilUzunluk);
            // Uzun kopya alanları var (aboutBig, marquee…) → sınırsız.
            e.Property(x => x.Value).HasColumnType("nvarchar(max)");
            // Aynı anahtarın aynı dilde iki değeri OLAMAZ — fallback mantığının dayanağı bu.
            e.HasIndex(x => new { x.Key, x.Lang }).IsUnique();
        });

        b.Entity<HeroRoleRow>(e =>
        {
            e.ToTable("HeroRole");
            e.Property(x => x.Lang).HasMaxLength(DilUzunluk);
            e.Property(x => x.Text).HasMaxLength(200);
            e.HasIndex(x => new { x.Lang, x.Order });
        });

        b.Entity<TerminalLogRow>(e =>
        {
            e.ToTable("TerminalLog");
            e.Property(x => x.Text).HasMaxLength(500);
            e.HasIndex(x => x.Order);
        });

        b.Entity<TechRow>(e =>
        {
            e.ToTable("Tech");
            e.Property(x => x.Name).HasMaxLength(100);
            e.Property(x => x.Note).HasMaxLength(200);
            e.HasIndex(x => x.Order);
        });

        b.Entity<FactRow>(e =>
        {
            e.ToTable("Fact");
            e.HasIndex(x => x.Order);
            CeviriKolonlari(e.OwnsOne(x => x.Label), "Label", 300);
        });

        b.Entity<GameRow>(e =>
        {
            e.ToTable("Game");
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Url).HasMaxLength(1000);
            e.Property(x => x.Image).HasMaxLength(1000);
            e.Property(x => x.Cover).HasMaxLength(50);
            e.HasIndex(x => x.Order);
            CeviriKolonlari(e.OwnsOne(x => x.Desc), "Desc", 1000);
        });

        b.Entity<DemoRow>(e =>
        {
            e.ToTable("Demo");
            e.Property(x => x.Path).HasMaxLength(200);
            e.Property(x => x.Name).HasMaxLength(200);
            // Etiketler EF ilkel koleksiyonu → tek JSON kolonu (ayrı tablo etmeyecek kadar küçük).
            e.PrimitiveCollection(x => x.Tags).HasColumnType("nvarchar(max)");
            e.Property(x => x.Html).HasColumnType("nvarchar(max)");
            e.HasIndex(x => x.Order);
            CeviriKolonlari(e.OwnsOne(x => x.Desc), "Desc", 1000);
        });

        b.Entity<RepoRow>(e =>
        {
            e.ToTable("Repo");
            e.Property(x => x.Name).HasMaxLength(200);
            e.Property(x => x.Lang).HasMaxLength(50);
            e.Property(x => x.Updated).HasMaxLength(50);
            e.Property(x => x.Url).HasMaxLength(1000);
            e.HasIndex(x => x.Order);
            CeviriKolonlari(e.OwnsOne(x => x.Desc), "Desc", 1000);
        });

        b.Entity<CoverPresetRow>(e =>
        {
            e.ToTable("CoverPreset");
            e.Property(x => x.Name).HasMaxLength(50);
            e.Property(x => x.Top).HasMaxLength(50);
            e.Property(x => x.Bottom).HasMaxLength(50);
            e.HasIndex(x => x.Name).IsUnique();
        });

        // --- Metrik tabloları ---------------------------------------------------
        // Kolon uzunlukları DAR tutuldu: bu tablolar içerikten çok daha hızlı büyür
        // ve tamamı istemciden gelen (yani güvenilmeyen) değerlerle doluyor.

        b.Entity<ZiyaretRow>(e =>
        {
            e.ToTable("Ziyaret");
            e.Property(x => x.ZiyaretciHash).HasMaxLength(32).IsFixedLength();
            e.Property(x => x.Yol).HasMaxLength(200);
            e.Property(x => x.Dil).HasMaxLength(DilUzunluk);
            e.Property(x => x.KaynakTipi).HasMaxLength(20);
            e.Property(x => x.KaynakHost).HasMaxLength(120);
            e.Property(x => x.Cihaz).HasMaxLength(20);

            // Her sorgu ve hem özetleme hem temizlik güne göre çalışır.
            e.HasIndex(x => x.Gun);
            // Tekil ziyaretçi sayımı (Gun, Hash) üzerinden DISTINCT alıyor.
            e.HasIndex(x => new { x.Gun, x.ZiyaretciHash });
        });

        b.Entity<OlayRow>(e =>
        {
            e.ToTable("Olay");
            e.Property(x => x.ZiyaretciHash).HasMaxLength(32).IsFixedLength();
            e.Property(x => x.Tip).HasMaxLength(20);
            e.Property(x => x.Deger).HasMaxLength(200);

            e.HasIndex(x => x.Gun);
            e.HasIndex(x => new { x.Gun, x.Tip });
        });

        b.Entity<GunlukOzetRow>(e =>
        {
            e.ToTable("GunlukOzet");
            e.Property(x => x.Tip).HasMaxLength(20);
            e.Property(x => x.Anahtar).HasMaxLength(200);

            // Aynı (gün, tip, anahtar) iki kez özetlenemez → rollup idempotent olur:
            // iş yarıda kalıp tekrar çalışsa bile sayılar ikiye katlanmaz.
            e.HasIndex(x => new { x.Gun, x.Tip, x.Anahtar }).IsUnique();
        });

        b.Entity<GunlukTuzRow>(e =>
        {
            e.ToTable("GunlukTuz");
            e.HasKey(x => x.Gun);
            e.Property(x => x.Tuz).HasMaxLength(64);
        });
    }

    /// <summary>
    /// <see cref="Localized"/>'ı sahip satırın kolonlarına yayar: DescTr/DescEn/DescRu.
    /// Ayrı tablo değil → yetim çeviri satırı fiziksel olarak imkânsız, okuma tek sorgu.
    /// </summary>
    private static void CeviriKolonlari<T>(
        Microsoft.EntityFrameworkCore.Metadata.Builders.OwnedNavigationBuilder<T, Localized> o,
        string onEk,
        int uzunluk) where T : class
    {
        o.Property(x => x.Tr).HasColumnName(onEk + "Tr").HasMaxLength(uzunluk).IsRequired().HasDefaultValue("");
        o.Property(x => x.En).HasColumnName(onEk + "En").HasMaxLength(uzunluk).IsRequired().HasDefaultValue("");
        o.Property(x => x.Ru).HasColumnName(onEk + "Ru").HasMaxLength(uzunluk).IsRequired().HasDefaultValue("");
    }
}
