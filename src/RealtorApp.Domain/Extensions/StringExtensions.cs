using System.Text;
using System.Text.RegularExpressions;

namespace RealtorApp.Domain.Extensions;

public static partial class StringExtensions
{
    [GeneratedRegex("([a-z])([A-Z])")]
    private static partial Regex PascalCaseRegex();

    public static string AddSpacesToPascalCase(this string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return PascalCaseRegex().Replace(text, "$1 $2");
    }
}
