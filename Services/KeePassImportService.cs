using System.Xml.Linq;
using PassTypePro.Models;

namespace PassTypePro.Services;

public sealed class KeePassImportService
{
    private readonly TotpService _totpService = new();

    public IReadOnlyList<SecretEntry> ImportXml(string filePath)
    {
        var document = XDocument.Load(filePath, LoadOptions.PreserveWhitespace);
        var root = document.Root ?? throw new InvalidOperationException("Die KeePass-Datei ist leer.");

        var entries = new List<SecretEntry>();
        var rootGroup = root.Element("Root")?.Element("Group");
        if (rootGroup is null)
        {
            throw new InvalidOperationException("Die KeePass-XML-Datei hat kein erwartetes Root/Group-Element.");
        }

        foreach (var group in rootGroup.DescendantsAndSelf("Group"))
        {
            var groupPath = BuildGroupPath(group);
            foreach (var entryElement in group.Elements("Entry"))
            {
                var entry = BuildEntry(entryElement, groupPath);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }
        }

        return entries;
    }

    private SecretEntry? BuildEntry(XElement entryElement, string groupPath)
    {
        var values = entryElement.Elements("String")
            .Select(item => new
            {
                Key = item.Element("Key")?.Value?.Trim() ?? string.Empty,
                Value = item.Element("Value")?.Value ?? string.Empty
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.Key))
            .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Last().Value, StringComparer.OrdinalIgnoreCase);

        var title = GetValue(values, "Title");
        var username = GetValue(values, "UserName", "User", "Benutzername");
        var password = GetValue(values, "Password", "Passwort");
        var notes = GetValue(values, "Notes", "Notizen");
        var url = GetValue(values, "URL", "Url");
        var totpSeed = GetTotpSeed(values, url);

        if (string.IsNullOrWhiteSpace(title) &&
            string.IsNullOrWhiteSpace(username) &&
            string.IsNullOrWhiteSpace(password) &&
            string.IsNullOrWhiteSpace(totpSeed) &&
            string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        return new SecretEntry
        {
            Name = string.IsNullOrWhiteSpace(title) ? BuildFallbackName(username, groupPath) : title,
            GroupPath = groupPath,
            Username = username,
            Url = url,
            Value = password,
            TotpSeed = totpSeed,
            Notes = notes,
            Source = "KeePass XML",
            SequenceTemplate = BuildSequence(username, password, totpSeed)
        };
    }

    private static string BuildGroupPath(XElement group)
    {
        var names = group.AncestorsAndSelf("Group")
            .Select(item => item.Element("Name")?.Value?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Reverse()
            .ToList();

        return string.Join(" / ", names!);
    }

    private static string BuildFallbackName(string username, string groupPath)
    {
        if (!string.IsNullOrWhiteSpace(username))
        {
            return username;
        }

        if (!string.IsNullOrWhiteSpace(groupPath))
        {
            return $"Import aus {groupPath}";
        }

        return "KeePass-Eintrag";
    }

    private static string BuildSequence(string username, string password, string totpSeed)
    {
        if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
        {
            return "{USERNAME}{TAB}{SECRET}";
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            return "{SECRET}";
        }

        if (!string.IsNullOrWhiteSpace(totpSeed))
        {
            return "{TOTP}";
        }

        if (!string.IsNullOrWhiteSpace(username))
        {
            return "{USERNAME}";
        }

        return "{SECRET}";
    }

    private string GetTotpSeed(IReadOnlyDictionary<string, string> values, string url)
    {
        var directSeed = GetValue(
            values,
            "TOTP Seed",
            "TOTPSeed",
            "TOTP",
            "OTP",
            "otp",
            "TimeOtp-Secret-Base32",
            "TimeOtp Secret",
            "KeeOtp",
            "KPOTP");

        if (!string.IsNullOrWhiteSpace(directSeed))
        {
            return _totpService.NormalizeSeed(directSeed);
        }

        var otpauthSource = GetValue(values, "otpauth", "OtpAuth", "OTP Auth");
        if (string.IsNullOrWhiteSpace(otpauthSource))
        {
            otpauthSource = url;
        }

        if (Uri.TryCreate(otpauthSource, UriKind.Absolute, out var uri) &&
            string.Equals(uri.Scheme, "otpauth", StringComparison.OrdinalIgnoreCase))
        {
            var query = uri.Query.TrimStart('?')
                .Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(
                    parts => Uri.UnescapeDataString(parts[0]),
                    parts => Uri.UnescapeDataString(parts[1]),
                    StringComparer.OrdinalIgnoreCase);

            if (query.TryGetValue("secret", out var secret))
            {
                return _totpService.NormalizeSeed(secret);
            }
        }

        return string.Empty;
    }

    private static string GetValue(IReadOnlyDictionary<string, string> values, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (values.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return string.Empty;
    }
}
