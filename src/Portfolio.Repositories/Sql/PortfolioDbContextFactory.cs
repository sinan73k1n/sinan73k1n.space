using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Portfolio.Repositories.Sql;

/// <summary>
/// Yalnız <c>dotnet ef</c> araçları için (migration üretme/ inceleme).
/// Uygulamanın çalışma anıyla İLGİSİ YOK — orada bağlantı dizesi DI'dan gelir.
///
/// <para>
/// Migration üretmek şemayı okumaya yetiyor, gerçek bir sunucuya bağlanmıyor;
/// bu yüzden varsayılan yer tutucu bir dize yeterli. Gerçekten bağlanmak gerekirse
/// (örn. <c>dotnet ef database update</c>) dizeyi ortam değişkeninden ver:
/// <code>PORTFOLIO_DB="Server=...;Database=portfoliodb;..." dotnet ef database update</code>
/// ⛔ Gerçek dize repoya YAZILMAZ.
/// </para>
/// </summary>
public sealed class PortfolioDbContextFactory : IDesignTimeDbContextFactory<PortfolioDbContext>
{
    public PortfolioDbContext CreateDbContext(string[] args)
    {
        var dize = Environment.GetEnvironmentVariable("PORTFOLIO_DB");
        if (string.IsNullOrWhiteSpace(dize))
            dize = "Server=(localdb)\\yer-tutucu;Database=portfoliodb;Trusted_Connection=True";

        var secenekler = new DbContextOptionsBuilder<PortfolioDbContext>()
            .UseSqlServer(dize)
            .Options;

        return new PortfolioDbContext(secenekler);
    }
}
