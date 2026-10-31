using System.Text;

namespace Localization.Tests;

/// <summary>Minimal RFC 4180 reader for game/localization/strings.csv.</summary>
public sealed class StringsCsv
{
    private StringsCsv(string[] header, List<string[]> rows) { Header = header; Rows = rows; }

    public string[] Header { get; }
    public List<string[]> Rows { get; }

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SwordInTheForest.sln"))) dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Repo root not found.");
    }

    public static StringsCsv Load()
    {
        var records = Parse(File.ReadAllText(Path.Combine(RepoRoot(), "game", "localization", "strings.csv")));
        return new StringsCsv(records[0], records.Skip(1).ToList());
    }

    private static List<string[]> Parse(string text)
    {
        var records = new List<string[]>();
        var fields = new List<string>();
        var field = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (quoted)
            {
                if (c == '"' && i + 1 < text.Length && text[i + 1] == '"') { field.Append('"'); i++; }
                else if (c == '"') quoted = false;
                else field.Append(c);
            }
            else if (c == '"') quoted = true;
            else if (c == ',') { fields.Add(field.ToString()); field.Clear(); }
            else if (c == '\r') { }
            else if (c == '\n') { fields.Add(field.ToString()); field.Clear(); records.Add(fields.ToArray()); fields.Clear(); }
            else field.Append(c);
        }
        if (field.Length > 0 || fields.Count > 0) { fields.Add(field.ToString()); records.Add(fields.ToArray()); }
        return records;
    }
}
