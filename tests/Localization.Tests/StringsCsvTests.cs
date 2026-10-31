using System.Text.RegularExpressions;
using Xunit;

namespace Localization.Tests;

public class StringsCsvTests
{
    private static readonly string[] Locales = { "en", "nl", "zh_CN" };

    [Fact]
    public void Header_HasKeysAndThreeLocales()
    {
        var table = StringsCsv.Load();
        Assert.Equal(new[] { "keys" }.Concat(Locales), table.Header);
    }

    [Fact]
    public void EveryKey_HasAllLocales_WithMatchingPlaceholders()
    {
        var table = StringsCsv.Load();
        Assert.NotEmpty(table.Rows);
        var problems = new List<string>();
        var seen = new HashSet<string>();
        foreach (var row in table.Rows)
        {
            var key = row[0];
            if (!seen.Add(key)) problems.Add($"{key}: duplicate key");
            if (!Regex.IsMatch(key, @"^(ui|combat|sword|upgrade|summary)\.[a-z0-9_.]+$"))
                problems.Add($"{key}: key must start with ui., combat., sword., upgrade. or summary.");
            if (row.Length != table.Header.Length) { problems.Add($"{key}: wrong column count"); continue; }
            var expected = Placeholders(row[1]);
            for (var i = 1; i < row.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(row[i])) problems.Add($"{key}: missing {table.Header[i]}");
                else if (!Placeholders(row[i]).SetEquals(expected)) problems.Add($"{key}: placeholders differ in {table.Header[i]}");
            }
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    private static HashSet<string> Placeholders(string value) =>
        Regex.Matches(value, @"\{([a-z_]+)\}").Select(m => m.Groups[1].Value).ToHashSet();
}
