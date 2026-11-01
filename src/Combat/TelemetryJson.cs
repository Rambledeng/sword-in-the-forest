using System.Globalization;
using System.Text;

namespace SwordInTheForest.Combat
{
    /// <summary>Formats telemetry records as single JSON lines matching docs/telemetry.schema.json.</summary>
    public static class TelemetryJson
    {
        public static string Format(TelemetryRecord r)
        {
            var sb = new StringBuilder(160);
            sb.Append("{\"run\":").Append(Quote(r.Run));
            sb.Append(",\"t\":").Append(r.TSeconds.ToString("0.###", CultureInfo.InvariantCulture));
            sb.Append(",\"attack\":").Append(Quote(r.Attack));
            sb.Append(",\"glint\":").Append(Quote(Name(r.Glint)));
            sb.Append(",\"lead_ms\":").Append(r.LeadMs.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"press_ms\":").Append(r.PressMs == null ? "null" : r.PressMs.Value.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"response\":").Append(Quote(Name(r.Response)));
            sb.Append(",\"outcome\":").Append(Quote(Name(r.Outcome)));
            sb.Append(",\"angle_deg\":").Append(r.AngleDeg == null ? "null" : r.AngleDeg.Value.ToString(CultureInfo.InvariantCulture));
            sb.Append('}');
            return sb.ToString();
        }

        public static string Name(GlintColor g) => g == GlintColor.Red ? "red" : "white";

        public static string Name(Response r) => r switch
        {
            Response.Deflect => "deflect",
            Response.Block => "block",
            Response.Roll => "roll",
            Response.Guard => "guard",
            _ => "none",
        };

        public static string Name(Outcome o) => o switch
        {
            Outcome.Success => "success",
            Outcome.Blocked => "blocked",
            Outcome.Evaded => "evaded",
            Outcome.Whiff => "whiff",
            _ => "hit",
        };

        public static string Quote(string s)
        {
            var sb = new StringBuilder(s.Length + 2).Append('"');
            foreach (var c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
