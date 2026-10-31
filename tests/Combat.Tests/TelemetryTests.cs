namespace Combat.Tests;

public class TelemetryTests
{
    private static JsonSchemaLite Schema()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "SwordInTheForest.sln"))) dir = dir.Parent;
        var path = Path.Combine(dir!.FullName, "docs", "telemetry.schema.json");
        return new JsonSchemaLite(File.ReadAllText(path));
    }

    [Fact]
    public void SpecExample_Validates()
    {
        var line = "{\"run\":\"r12\",\"t\":184.2,\"attack\":\"saber_chop\",\"glint\":\"white\",\"lead_ms\":420,\"press_ms\":365,\"response\":\"deflect\",\"outcome\":\"success\"}";
        Assert.Empty(Schema().ValidateLine(line));
    }

    [Fact]
    public void EveryFormattedCombination_Validates()
    {
        var schema = Schema();
        foreach (var glint in Enum.GetValues<GlintColor>())
        foreach (var response in Enum.GetValues<Response>())
        foreach (var outcome in Enum.GetValues<Outcome>())
        foreach (var press in new int?[] { null, 0, 365, 1200 })
        {
            var line = TelemetryJson.Format(new TelemetryRecord("r1", 12.3456, "saber_slash_1", glint, 420, press, response, outcome));
            Assert.Empty(schema.ValidateLine(line));
            Assert.DoesNotContain('\n', line);
        }
    }

    [Fact]
    public void RecordFromThreatWindow_Validates()
    {
        var attack = new AttackSpec("saber_chop", GlintColor.White, 700, 1200, 520, 400, 600, 900);
        var w = new ThreatWindow(attack, 5000, 520);
        w.RecordAction(DefenseAction.Guard, 5310);
        w.Resolve(new ImpactState(true, false, true));
        var line = TelemetryJson.Format(TelemetryRecord.From(w, "r7", 1000));
        Assert.Equal("{\"run\":\"r7\",\"t\":4,\"attack\":\"saber_chop\",\"glint\":\"white\",\"lead_ms\":520,\"press_ms\":310,\"response\":\"deflect\",\"outcome\":\"success\"}", line);
        Assert.Empty(Schema().ValidateLine(line));
    }

    [Theory]
    [InlineData("{\"run\":\"r1\",\"t\":1,\"attack\":\"a\",\"glint\":\"white\",\"lead_ms\":420,\"response\":\"none\",\"outcome\":\"hit\"}", "missing 'press_ms'")]
    [InlineData("{\"run\":\"r1\",\"t\":1,\"attack\":\"a\",\"glint\":\"blue\",\"lead_ms\":420,\"press_ms\":null,\"response\":\"none\",\"outcome\":\"hit\"}", "$.glint")]
    [InlineData("{\"run\":\"r1\",\"t\":1,\"attack\":\"a\",\"glint\":\"red\",\"lead_ms\":420.5,\"press_ms\":null,\"response\":\"none\",\"outcome\":\"hit\"}", "$.lead_ms")]
    [InlineData("{\"run\":\"r1\",\"t\":1,\"attack\":\"a\",\"glint\":\"red\",\"lead_ms\":420,\"press_ms\":-3,\"response\":\"none\",\"outcome\":\"hit\"}", "$.press_ms")]
    [InlineData("{\"run\":\"r1\",\"t\":1,\"attack\":\"a\",\"glint\":\"red\",\"lead_ms\":420,\"press_ms\":null,\"response\":\"parry\",\"outcome\":\"hit\"}", "$.response")]
    [InlineData("{\"run\":\"r1\",\"t\":1,\"attack\":\"a\",\"glint\":\"red\",\"lead_ms\":420,\"press_ms\":null,\"response\":\"none\",\"outcome\":\"hit\",\"x\":1}", "unexpected property 'x'")]
    [InlineData("{\"run\":\"r1\",\"t\":1,\"attack\":\"Saber Chop\",\"glint\":\"red\",\"lead_ms\":420,\"press_ms\":null,\"response\":\"none\",\"outcome\":\"hit\"}", "$.attack")]
    [InlineData("{\"run\":\"r1\",", "JSON error")]
    public void BadLines_AreRejected(string line, string expectedProblem)
    {
        var problems = Schema().ValidateLine(line);
        Assert.Contains(problems, p => p.Contains(expectedProblem));
    }

    [Fact]
    public void Quote_EscapesControlCharacters()
    {
        Assert.Equal("\"a\\\"b\\\\c\\n\"", TelemetryJson.Quote("a\"b\\c\n"));
        Assert.Equal("a\"b\\c\n", JsonLite.Parse(TelemetryJson.Quote("a\"b\\c\n")));
    }
}
