using System.Text.RegularExpressions;
using Xunit;

namespace Localization.Tests;

/// <summary>Scenes may only show translation keys, never literal player-facing text.</summary>
public class SceneTextTests
{
    private static readonly Regex TextProperty =
        new(@"^(text|tooltip_text|placeholder_text)\s*=\s*""(?<value>(?:[^""\\]|\\.)*)""", RegexOptions.Multiline);

    [Fact]
    public void SceneText_IsAlwaysAKnownKey()
    {
        var keys = StringsCsv.Load().Rows.Select(r => r[0]).ToHashSet();
        var gameDir = Path.Combine(StringsCsv.RepoRoot(), "game");
        var problems = new List<string>();
        foreach (var scene in Directory.EnumerateFiles(Path.Combine(gameDir, "scenes"), "*.tscn", SearchOption.AllDirectories))
        {
            foreach (Match m in TextProperty.Matches(File.ReadAllText(scene)))
            {
                var value = m.Groups["value"].Value;
                if (value.Length > 0 && !keys.Contains(value))
                    problems.Add($"{Path.GetRelativePath(gameDir, scene)}: '{value}' is not a key in strings.csv");
            }
        }
        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }
}
