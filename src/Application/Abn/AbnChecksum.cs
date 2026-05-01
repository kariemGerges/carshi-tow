namespace CarshiTow.Application.Abn;

/// <summary>ABN weighted checksum validation (Australian Business Register algorithm).</summary>
public static class AbnChecksum
{
    private static ReadOnlySpan<int> Weights => DefaultWeights;

    private static readonly int[] DefaultWeights = [10, 1, 3, 5, 7, 9, 11, 13, 15, 17, 19];

    /// <summary>Normalises to exactly 11 digits or returns null.</summary>
    public static string? NormalizeToDigits(string? abnInput)
    {
        if (string.IsNullOrWhiteSpace(abnInput))
        {
            return null;
        }

        Span<char> buffer = stackalloc char[16];
        var len = 0;
        foreach (var c in abnInput.Trim())
        {
            if (!char.IsDigit(c))
            {
                continue;
            }

            buffer[len++] = c;
            if (len >= buffer.Length)
            {
                break;
            }
        }

        if (len != 11)
        {
            return null;
        }

        return new string(buffer[..len]);
    }

    /// <summary>Returns true only when checksum matches for an 11-digit string.</summary>
    public static bool IsValid(ReadOnlySpan<char> elevenDigits)
    {
        if (elevenDigits.Length != 11)
        {
            return false;
        }

        Span<int> d = stackalloc int[11];
        for (var i = 0; i < 11; i++)
        {
            if (!char.IsDigit(elevenDigits[i]))
            {
                return false;
            }

            d[i] = elevenDigits[i] - '0';
        }

        d[0] -= 1;
        var sum = 0;
        for (var i = 0; i < 11; i++)
        {
            sum += d[i] * Weights[i];
        }

        return sum % 89 == 0;
    }

    /// <inheritdoc cref="NormalizeToDigits"/>
    public static bool TryValidate(string? abnInput, out string normalized)
    {
        normalized = NormalizeToDigits(abnInput) ?? string.Empty;
        return normalized.Length == 11 && IsValid(normalized);
    }

    /// <summary>
    /// Pseudo-random 11-digit ABN that passes <see cref="IsValid"/> (integration tests / local seed rows only). Never use as real business identifiers.
    /// </summary>
    public static string CreateRandomChecksumValidAbn(Random? random = null)
    {
        random ??= Random.Shared;
        Span<char> s = stackalloc char[11];

        for (var attempt = 0; attempt < 5000; attempt++)
        {
            s[0] = (char)('1' + random.Next(0, 9));
            for (var i = 1; i < 10; i++)
            {
                s[i] = (char)('0' + random.Next(0, 10));
            }

            for (var last = 0; last <= 9; last++)
            {
                s[10] = (char)('0' + last);
                if (IsValid(s))
                {
                    return new string(s);
                }
            }
        }

        throw new InvalidOperationException("Could not synthesize checksum-valid ABN.");
    }
}
