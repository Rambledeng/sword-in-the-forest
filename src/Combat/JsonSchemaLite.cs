using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// Validates a parsed JSON value against the subset of JSON Schema the telemetry schema uses:
    /// type (string or list), required, properties, additionalProperties: false, enum, minimum,
    /// maximum, minLength and pattern.
    /// </summary>
    public sealed class JsonSchemaLite
    {
        private readonly Dictionary<string, object?> _schema;

        public JsonSchemaLite(string schemaJson)
        {
            _schema = JsonLite.Parse(schemaJson) as Dictionary<string, object?>
                ?? throw new System.FormatException("Schema must be a JSON object.");
        }

        /// <summary>Returns the problems found; an empty list means the line is valid.</summary>
        public List<string> ValidateLine(string jsonLine)
        {
            var problems = new List<string>();
            object? value;
            try { value = JsonLite.Parse(jsonLine); }
            catch (System.FormatException e) { problems.Add(e.Message); return problems; }
            Validate(_schema, value, "$", problems);
            return problems;
        }

        private static void Validate(Dictionary<string, object?> schema, object? value, string path, List<string> problems)
        {
            if (schema.TryGetValue("type", out var type) && type != null)
            {
                var types = type is List<object?> list ? list.Select(t => (string)t!).ToList() : new List<string> { (string)type };
                if (!types.Any(t => HasType(value, t)))
                {
                    problems.Add($"{path}: expected {string.Join("|", types)}, got {Describe(value)}");
                    return;
                }
            }
            if (schema.TryGetValue("enum", out var en) && en is List<object?> options && !options.Any(o => Equals(o, value)))
                problems.Add($"{path}: {Describe(value)} is not one of the allowed values");
            if (value is double d)
            {
                if (schema.TryGetValue("minimum", out var min) && min is double mn && d < mn) problems.Add($"{path}: {d} < minimum {mn}");
                if (schema.TryGetValue("maximum", out var max) && max is double mx && d > mx) problems.Add($"{path}: {d} > maximum {mx}");
            }
            if (value is string s)
            {
                if (schema.TryGetValue("minLength", out var ml) && ml is double m && s.Length < m) problems.Add($"{path}: shorter than {m}");
                if (schema.TryGetValue("pattern", out var p) && p is string pattern && !Regex.IsMatch(s, pattern)) problems.Add($"{path}: '{s}' does not match {pattern}");
            }
            if (value is Dictionary<string, object?> obj)
            {
                var props = schema.TryGetValue("properties", out var pr) ? pr as Dictionary<string, object?> : null;
                if (schema.TryGetValue("required", out var req) && req is List<object?> required)
                    foreach (string? key in required)
                        if (key != null && !obj.ContainsKey(key)) problems.Add($"{path}: missing '{key}'");
                foreach (var kv in obj)
                {
                    if (props != null && props.TryGetValue(kv.Key, out var sub) && sub is Dictionary<string, object?> subSchema)
                        Validate(subSchema, kv.Value, $"{path}.{kv.Key}", problems);
                    else if (schema.TryGetValue("additionalProperties", out var ap) && ap is bool allowed && !allowed)
                        problems.Add($"{path}: unexpected property '{kv.Key}'");
                }
            }
        }

        private static bool HasType(object? value, string type) => type switch
        {
            "null" => value == null,
            "boolean" => value is bool,
            "string" => value is string,
            "number" => value is double,
            "integer" => value is double d && d == System.Math.Floor(d) && !double.IsInfinity(d),
            "object" => value is Dictionary<string, object?>,
            "array" => value is List<object?>,
            _ => false,
        };

        private static string Describe(object? value) => value switch
        {
            null => "null",
            string s => $"'{s}'",
            double d => d.ToString(CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => value.GetType().Name,
        };
    }
}
