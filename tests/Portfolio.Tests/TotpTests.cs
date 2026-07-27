using System.Text;
using Portfolio.Services.Auth;

namespace Portfolio.Tests;

/// <summary>
/// TOTP doğruluğu — <b>RFC 6238 Appendix B resmî test vektörleri</b>.
/// "Kendi kodumla kendi kodumu doğrulamak" değil: standardın yayımladığı beklenen
/// çıktılarla karşılaştırılıyor. Authenticator uygulamaları (Google Authenticator,
/// Authy) da aynı vektörleri sağlar → uyum garanti.
/// Vektörler 8 haneli; bizim uygulama 6 hane ürettiği için son 6 hane beklenir
/// (kesme aynı sayıdan yapılır: bin % 10^6).
/// </summary>
public class TotpTests
{
    // RFC 6238 seed (SHA1): ASCII "12345678901234567890"
    private static readonly byte[] Seed = Encoding.ASCII.GetBytes("12345678901234567890");
    private const int Step = 30;

    [Theory]
    // zaman (T, saniye)     RFC'nin 8 haneli beklentisi   → 6 haneli karşılığı
    [InlineData(59L,          "94287082", "287082")]
    [InlineData(1111111109L,  "07081804", "081804")]
    [InlineData(1111111111L,  "14050471", "050471")]
    [InlineData(1234567890L,  "89005924", "005924")]
    [InlineData(2000000000L,  "69279037", "279037")]
    [InlineData(20000000000L, "65353130", "353130")]
    public void Compute_RFC6238_vektorleriyle_uyusuyor(long zaman, string rfc8Hane, string beklenen6Hane)
    {
        var sayac = zaman / Step;

        var uretilen = Totp.Compute(Seed, sayac);

        Assert.Equal(beklenen6Hane, uretilen);
        Assert.EndsWith(uretilen, rfc8Hane);   // 6 hane, 8 hanenin sonu olmalı
    }

    [Fact]
    public void Verify_gecerli_kodu_kabul_eder()
    {
        var secret = Totp.GenerateSecret();
        var anahtar = Totp.Base32Decode(secret);
        var simdi = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / Step;

        var kod = Totp.Compute(anahtar, simdi);

        Assert.True(Totp.Verify(secret, kod));
    }

    [Theory]
    [InlineData(-1)]   // bir önceki 30sn penceresi
    [InlineData(1)]    // bir sonraki 30sn penceresi
    public void Verify_bir_adim_saat_kaymasini_tolere_eder(int kayma)
    {
        var secret = Totp.GenerateSecret();
        var anahtar = Totp.Base32Decode(secret);
        var sayac = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / Step + kayma;

        var kod = Totp.Compute(anahtar, sayac);

        Assert.True(Totp.Verify(secret, kod, window: 1));
    }

    [Fact]
    public void Verify_pencere_disini_reddeder()
    {
        var secret = Totp.GenerateSecret();
        var anahtar = Totp.Base32Decode(secret);
        // 5 adım (2.5 dk) önceki kod artık geçerli olmamalı
        var eski = Totp.Compute(anahtar, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / Step - 5);

        Assert.False(Totp.Verify(secret, eski, window: 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]      // 5 hane
    [InlineData("1234567")]    // 7 hane
    [InlineData("abcdef")]     // rakam değil
    [InlineData("12 34 56")]   // boşluklu
    public void Verify_bicimsiz_girdiyi_reddeder(string? kod)
    {
        Assert.False(Totp.Verify(Totp.GenerateSecret(), kod));
    }

    [Fact]
    public void Verify_bozuk_secret_ile_patlamaz_false_doner()
    {
        // Base32'de olmayan karakterler → FormatException yutulmalı, false dönmeli
        Assert.False(Totp.Verify("bu-gecerli-base32-degil!!!", "123456"));
    }

    [Fact]
    public void GenerateSecret_160_bit_uretir_ve_her_seferinde_farkli()
    {
        var a = Totp.GenerateSecret();
        var b = Totp.GenerateSecret();

        Assert.Equal(20, Totp.Base32Decode(a).Length);   // 160 bit
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Base32_gidis_donus_ayni_veriyi_verir()
    {
        var veri = new byte[] { 0x00, 0x7f, 0x80, 0xff, 0x10, 0x2a, 0x3b, 0x4c, 0x5d, 0x6e };

        var kodlanmis = Totp.Base32Encode(veri);

        Assert.Equal(veri, Totp.Base32Decode(kodlanmis));
    }

    [Fact]
    public void OtpAuthUri_authenticator_bicimini_uretir()
    {
        var uri = Totp.OtpAuthUri("Portfolyo", "admin", "GEZDGNBVGY3TQOJQ");

        Assert.StartsWith("otpauth://totp/Portfolyo:admin?", uri);
        Assert.Contains("secret=GEZDGNBVGY3TQOJQ", uri);
        Assert.Contains("algorithm=SHA1", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
    }
}
