using System.Text.RegularExpressions;

namespace RealtorApp.Domain.Helpers;

public static class SearchResultTemplateHelper
{
    private const string TemplateStartVariable = "{{Match}}";
    private const string TemplateEndVariable = "{{/Match}}";

    public static Regex CreateSearchTermRegex(string searchTerm)
    {
        var pattern = Regex.Escape(searchTerm);
        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    public static string AddTagsAroundSearchTermMatch(string toTemplate, Regex searchTermRegex)
    {
        if (string.IsNullOrEmpty(toTemplate))
        {
            return toTemplate;
        }

        return searchTermRegex.Replace(toTemplate, match => $"{TemplateStartVariable}{match.Value}{TemplateEndVariable}");
    }
}
