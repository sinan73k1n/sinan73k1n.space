using System.Security.Cryptography;
using System.Text;

namespace Portfolio.Services.Auth;

/// <summary>
/// RFC 6238 TOTP (Google Authenticator / Authy uyumlu): HMAC-SHA1, 30sn adım, 6 hane.
/// <b>Harici bağımlılık YOK</b> (yalnız BCL HMAC) — tedarik zinciri yüzeyi küçük kalsın
/// (AdminPanel projesinden devralındı; RFC 6238 test vektörleriyle doğrulanmıştır). Doğruluk RFC 6238 test vektörleriyle doğrulandı.
/// </summary>
public static class Totp
{
    private const int Step = 30;
    private const int Digits = 6;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Kodu doğrular. <paramref name="window"/>=1 → saat kayması için ±1 adım (±30sn) tolerans.
    /// Sabit-zamanlı karşılaştırma (zamanlama sızıntısı yok).
    /// </summary>
    public static bool Verify(string base32Secret, string? code, int window = 1)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        code = code.Trim();
        if (code.Length != Digits || !code.All(char.IsDigit)) return false;

        byte[] key;
        try { key = Base32Decode(base32Secret); }
        catch { return false; }

        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / Step;
        for (var i = -window; i <= window; i++)
            if (FixedEquals(Compute(key, counter + i), code))
                return true;
        return false;
    }

    /// <summary>Belirli bir sayaç için 6 haneli kod (RFC 4226 HOTP).</summary>
    public static string Compute(byte[] key, long counter)
    {
        var ctr = new byte[8];
        for (var i = 7; i >= 0; i--) { ctr[i] = (byte)(counter & 0xff); counter >>= 8; }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(ctr);
        var offset = hash[^1] & 0x0f;
        var bin = ((hash[offset] & 0x7f) << 24)
                | (hash[offset + 1] << 16)
                | (hash[offset + 2] << 8)
                | hash[offset + 3];
        var otp = bin % (int)Math.Pow(10, Digits);
        return otp.ToString().PadLeft(Digits, '0');
    }

    /// <summary>Yeni rastgele secret (20 byte = 160 bit, base32).</summary>
    public static string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encode(bytes);
    }

    /// <summary>Authenticator uygulamasına QR/elle giriş için otpauth URI'si.</summary>
    public static string OtpAuthUri(string issuer, string account, string base32Secret)
        => $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}"
         + $"?secret={base32Secret}&issuer={Uri.EscapeDataString(issuer)}&algorithm=SHA1&digits={Digits}&period={Step}";

    private static bool FixedEquals(string a, string b)
        => a.Length == b.Length && CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(a), Encoding.ASCII.GetBytes(b));

    public static byte[] Base32Decode(string input)
    {
        input = input.TrimEnd('=').ToUpperInvariant().Replace(" ", "");
        var bits = 0; var value = 0;
        var output = new List<byte>(input.Length * 5 / 8);
        foreach (var c in input)
        {
            var idx = Base32Alphabet.IndexOf(c);
            if (idx < 0) throw new FormatException($"Geçersiz base32 karakteri: {c}");
            value = (value << 5) | idx;
            bits += 5;
            if (bits >= 8) { output.Add((byte)((value >> (bits - 8)) & 0xff)); bits -= 8; }
        }
        return output.ToArray();
    }

    public static string Base32Encode(byte[] data)
    {
        var sb = new StringBuilder((data.Length + 4) / 5 * 8);
        var bits = 0; var value = 0;
        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;
            while (bits >= 5) { sb.Append(Base32Alphabet[(value >> (bits - 5)) & 31]); bits -= 5; }
        }
        if (bits > 0) sb.Append(Base32Alphabet[(value << (5 - bits)) & 31]);
        return sb.ToString();
    }
}
