using System.Text;

namespace AgentPort.PlatformApi.Infrastructure;

public static class SlugGenerator
{
    public static string FromName(string value, string fallback = "item")
    {
        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator && (char.IsWhiteSpace(character) || character is '-' or '_' or '.'))
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        var slug = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? fallback : slug;
    }
}
