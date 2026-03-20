using System.Security.Cryptography;
using System.Text;

namespace PassTypePro.Services;

public sealed class TotpService
{
    public bool CanGenerate(string seed)
    {
        return TryDecodeBase32(seed, out _);
    }

    public string GenerateCode(string seed, DateTimeOffset? timestamp = null, int digits = 6, int periodSeconds = 30)
    {
        if (!TryDecodeBase32(seed, out var key))
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

    private static bool TryDecodeBase32(string value, out byte[] bytes)
    {
        bytes = [];

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .TrimEnd('=')
            .ToUpperInvariant();

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
}
