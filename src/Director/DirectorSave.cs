using System;
using System.Globalization;
using System.IO;

namespace SwordInTheForest.Director
{
    /// <summary>
    /// Text save format for the belief, so the estimate carries across runs:
    /// <code>
    /// sword-director 1
    /// grid &lt;signature&gt;
    /// observations &lt;n&gt;
    /// weights &lt;count&gt;
    /// &lt;one weight per line&gt;
    /// </code>
    /// </summary>
    public static class DirectorSave
    {
        private const string Magic = "sword-director 1";

        public static void Write(ReactionDirector director, TextWriter writer)
        {
            writer.WriteLine(Magic);
            writer.WriteLine("grid " + director.Grid.Signature);
            writer.WriteLine("observations " + director.Observations.ToString(CultureInfo.InvariantCulture));
            writer.WriteLine("weights " + director.Weights.Count.ToString(CultureInfo.InvariantCulture));
            foreach (var w in director.Weights) writer.WriteLine(w.ToString("G6", CultureInfo.InvariantCulture));
        }

        public static string ToText(ReactionDirector director)
        {
            using var sw = new StringWriter(CultureInfo.InvariantCulture);
            Write(director, sw);
            return sw.ToString();
        }

        /// <summary>Reads a saved belief; null if the text is not a save for this grid (then use the prior).</summary>
        public static ReactionDirector? Read(DirectorGrid grid, TextReader reader, DirectorOptions? options = null)
        {
            try
            {
                if (reader.ReadLine()?.Trim() != Magic) return null;
                if (reader.ReadLine()?.Trim() != "grid " + grid.Signature) return null;
                var obs = Field(reader.ReadLine(), "observations");
                var count = Field(reader.ReadLine(), "weights");
                if (obs < 0 || count != grid.CellCount) return null;
                var w = new double[count];
                for (var i = 0; i < count; i++)
                {
                    var line = reader.ReadLine();
                    if (line == null || !double.TryParse(line, NumberStyles.Float, CultureInfo.InvariantCulture, out w[i]) ||
                        w[i] < 0 || double.IsNaN(w[i]) || double.IsInfinity(w[i])) return null;
                }
                return new ReactionDirector(grid, w, obs, options);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static ReactionDirector? FromText(DirectorGrid grid, string text, DirectorOptions? options = null) =>
            Read(grid, new StringReader(text), options);

        private static int Field(string? line, string name)
        {
            if (line == null || !line.StartsWith(name + " ", StringComparison.Ordinal)) return -1;
            return int.TryParse(line.Substring(name.Length + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v) ? v : -1;
        }
    }
}
