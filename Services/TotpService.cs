using System.Security.Cryptography;
using System.Text;

namespace PassTypePro.Services;

public sealed class TotpService
{
    public bool CanGenerate(string seed)
    {
        return TryDecodeBase32(NormalizeSeed(seed), out _);
    }

    public string GenerateCode(string seed, DateTimeOffset? timestamp = null, int digits = 6, int periodSeconds = 30)
    {
        if (!TryDecodeBase32(NormalizeSeed(seed), out var key))
        {
            throw new InvalidOperationException("Der TOTP-Seed ist ungueltig.");
        }

        var unixTime = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        var counter = unixTime / periodSeconds;
        Span<byte> counterBytes = stackalloc byte[8];

        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary =
            ((hash[offset] & 0x7f) << 24) |
            (hash[offset + 1] << 16) |
            (hash[offset + 2] << 8) |
            hash[offset + 3];

        var otp = binary % (int)Math.Pow(10, digits);
        return otp.ToString(new string('0', digits));
    }

    public int GetRemainingSeconds(DateTimeOffset? timestamp = null, int periodSeconds = 30)
    {
        var unixTime = (timestamp ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
        var elapsed = (int)(unixTime % periodSeconds);
        return periodSeconds - elapsed;
    }

    public string NormalizeSeed(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed))
        {
            return string.Empty;
        }

        var normalized = seed.Trim();

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var otpauthUri) &&
            string.Equals(otpauthUri.Scheme, "otpauth", StringComparison.OrdinalIgnoreCase))
        {
            var otpauthSecret = ExtractSecretFromQuery(otpauthUri.Query);
            if (!string.IsNullOrWhiteSpace(otpauthSecret))
            {
                normalized = otpauthSecret;
            }
        }
        else
        {
            var secretFromText = ExtractSecretFromText(normalized);
            if (!string.IsNullOrWhiteSpace(secretFromText))
            {
                normalized = secretFromText;
            }
        }

        return normalized
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .TrimEnd('=')
            .ToUpperInvariant();
    }

    private static bool TryDecodeBase32(string value, out byte[] bytes)
    {
        bytes = [];

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim();

        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var buffer = 0;
        var bitsLeft = 0;
        var output = new List<byte>();

        foreach (var character in normalized)
        {
            var index = alphabet.IndexOf(character);
            if (index < 0)
            {
                return false;
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)((buffer >> bitsLeft) & 0xff));
            }
        }

        bytes = [.. output];
        return bytes.Length > 0;
    }

    private static string ExtractSecretFromText(string value)
    {
        var markers = new[]
        {
            "secret=",
            "key=",
            "totpseed=",
            "totp=",
            "otp=",
            "TimeOtp-Secret-Base32=",
            "TimeOtp Secret="
        };

        foreach (var marker in markers)
        {
            var index = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                continue;
            }

            var start = index + marker.Length;
            var end = value.IndexOfAny(['&', ';', '\r', '\n'], start);
            var candidate = end >= 0 ? value[start..end] : value[start..];
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return Uri.UnescapeDataString(candidate.Trim().Trim('"'));
            }
        }

        return value;
    }

    private static string ExtractSecretFromQuery(string query)
    {
        foreach (var part in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pieces = part.Split('=', 2);
            if (pieces.Length != 2)
            {
                continue;
            }

            if (string.Equals(Uri.UnescapeDataString(pieces[0]), "secret", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pieces[1]);
            }
        }

        return string.Empty;
    }
}
