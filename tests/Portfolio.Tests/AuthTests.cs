using Microsoft.Extensions.Options;
using Portfolio.Services.Auth;

namespace Portfolio.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_dogru_sifreyi_dogrular()
    {
        var hash = PasswordHasher.Hash("çok-gizli-parola-2026");

        Assert.True(PasswordHasher.Verify("çok-gizli-parola-2026", hash));
    }

    [Fact]
    public void Hash_yanlis_sifreyi_reddeder()
    {
        var hash = PasswordHasher.Hash("dogru-parola-uzun");

        Assert.False(PasswordHasher.Verify("yanlis-parola-uzun", hash));
    }

    [Fact]
    public void Hash_ayni_sifre_icin_her_seferinde_farkli_cikti_verir()
    {
        // Salt rastgele → aynı parola iki farklı hash üretmeli (rainbow table savunması)
        var a = PasswordHasher.Hash("ayni-parola");
        var b = PasswordHasher.Hash("ayni-parola");

        Assert.NotEqual(a, b);
        Assert.True(PasswordHasher.Verify("ayni-parola", a));
        Assert.True(PasswordHasher.Verify("ayni-parola", b));
    }

    [Fact]
    public void Hash_beklenen_bicimde_ve_yeterli_iterasyonla()
    {
        var parcalar = PasswordHasher.Hash("x").Split('$');

        Assert.Equal(4, parcalar.Length);
        Assert.Equal("pbkdf2", parcalar[0]);
        Assert.True(int.Parse(parcalar[1]) >= 210_000, "OWASP tavsiyesi altına düşmemeli");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("bozuk")]
    [InlineData("pbkdf2$abc$salt$hash")]        // iterasyon sayı değil
    [InlineData("md5$1000$salt$hash")]          // desteklenmeyen algoritma
    [InlineData("pbkdf2$1000$!!!$!!!")]         // base64 değil
    public void Verify_bozuk_hash_ile_patlamaz_false_doner(string? hash)
    {
        Assert.False(PasswordHasher.Verify("herhangi", hash));
    }
}

public class AdminAuthenticatorTests
{
    private const string Parola = "test-parolasi-123";

    private static (IAdminAuthenticator kimlik, string secret) Kur(bool aktif = true)
    {
        var secret = Totp.GenerateSecret();
        var opt = new AuthOptions
        {
            Username = "admin",
            PasswordHash = aktif ? PasswordHasher.Hash(Parola) : null,
            TotpSecret = aktif ? secret : null
        };
        return (new AdminAuthenticator(Options.Create(opt)), secret);
    }

    private static string GecerliKod(string secret) =>
        Totp.Compute(Totp.Base32Decode(secret), DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);

    [Fact]
    public void Her_iki_faktor_dogruysa_kabul_eder()
    {
        var (kimlik, secret) = Kur();

        Assert.True(kimlik.Validate("admin", Parola, GecerliKod(secret)));
    }

    [Fact]
    public void Sifre_dogru_TOTP_yanlissa_REDDEDER()
    {
        var (kimlik, _) = Kur();

        Assert.False(kimlik.Validate("admin", Parola, "000000"));
    }

    [Fact]
    public void TOTP_dogru_sifre_yanlissa_REDDEDER()
    {
        var (kimlik, secret) = Kur();

        Assert.False(kimlik.Validate("admin", "yanlis-parola", GecerliKod(secret)));
    }

    [Fact]
    public void Kullanici_adi_yanlissa_reddeder()
    {
        var (kimlik, secret) = Kur();

        Assert.False(kimlik.Validate("baskasi", Parola, GecerliKod(secret)));
    }

    [Fact]
    public void Kullanici_adi_buyuk_kucuk_harf_duyarsiz()
    {
        var (kimlik, secret) = Kur();

        Assert.True(kimlik.Validate("ADMIN", Parola, GecerliKod(secret)));
    }

    [Fact]
    public void Auth_kapaliyken_dogru_bilgiyle_bile_reddeder()
    {
        var (kimlik, _) = Kur(aktif: false);

        Assert.False(kimlik.Enabled);
        Assert.False(kimlik.Validate("admin", Parola, "123456"));
    }
}

public class LoginThrottleTests
{
    private static ILoginThrottle Kur(int maxDeneme = 3, int kilitSn = 60) =>
        new LoginThrottle(Options.Create(new AuthOptions
        {
            MaxAttempts = maxDeneme,
            LockoutSeconds = kilitSn,
            MaxLockoutSeconds = 3600
        }));

    [Fact]
    public void Basta_kilitli_degil()
    {
        Assert.Null(Kur().KilitliMi("1.2.3.4"));
    }

    [Fact]
    public void Sinirin_altinda_kilitlemez()
    {
        var t = Kur(maxDeneme: 3);

        t.BasarisizDeneme("ip"); t.BasarisizDeneme("ip");

        Assert.Null(t.KilitliMi("ip"));
    }

    [Fact]
    public void Sinira_ulasinca_kilitler()
    {
        var t = Kur(maxDeneme: 3);

        for (var i = 0; i < 3; i++) t.BasarisizDeneme("ip");

        var kalan = t.KilitliMi("ip");
        Assert.NotNull(kalan);
        Assert.InRange(kalan!.Value.TotalSeconds, 1, 60);
    }

    [Fact]
    public void Kilit_suresi_her_turda_katlanir()
    {
        var t = Kur(maxDeneme: 1, kilitSn: 10);

        t.BasarisizDeneme("ip");
        var birinci = t.KilitliMi("ip")!.Value.TotalSeconds;

        // kilit turu artsın diye ikinci kez tetikle (kilit doldu varsayımı yerine
        // doğrudan yeni başarısızlık — sayaç sıfırlandığı için tekrar tetikler)
        t.BasarisizDeneme("ip");
        var ikinci = t.KilitliMi("ip")!.Value.TotalSeconds;

        Assert.True(ikinci > birinci, $"ikinci kilit ({ikinci}s) birinciden ({birinci}s) uzun olmalı");
    }

    [Fact]
    public void Basarili_giriste_sayac_sifirlanir()
    {
        var t = Kur(maxDeneme: 3);
        t.BasarisizDeneme("ip"); t.BasarisizDeneme("ip");

        t.Sifirla("ip");
        t.BasarisizDeneme("ip");        // sıfırlandıysa bu 1. deneme

        Assert.Null(t.KilitliMi("ip"));
    }

    [Fact]
    public void Kilit_IP_bazli_digerlerini_etkilemez()
    {
        var t = Kur(maxDeneme: 2);

        t.BasarisizDeneme("saldirgan"); t.BasarisizDeneme("saldirgan");

        Assert.NotNull(t.KilitliMi("saldirgan"));
        Assert.Null(t.KilitliMi("mesru-kullanici"));
    }
}
