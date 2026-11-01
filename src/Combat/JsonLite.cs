using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SwordInTheForest.Combat
{
    /// <summary>
    /// A small JSON reader (netstandard2.1 has no built-in one). Objects become
    /// Dictionary&lt;string, object?&gt;, arrays List&lt;object?&gt;, numbers double.
    /// </summary>
    public static class JsonLite
    {
        public static object? Parse(string text)
        {
            var i = 0;
            var value = ReadValue(text, ref i);
            SkipWhitespace(text, ref i);
            if (i != text.Length) throw Error(text, i, "trailing characters");
            return value;
        }

        private static object? ReadValue(string s, ref int i)
        {
            SkipWhitespace(s, ref i);
            if (i >= s.Length) throw Error(s, i, "unexpected end");
            var c = s[i];
            if (c == '{') return ReadObject(s, ref i);
            if (c == '[') return ReadArray(s, ref i);
            if (c == '"') return ReadString(s, ref i);
            if (c == 't') return ReadLiteral(s, ref i, "true", true);
            if (c == 'f') return ReadLiteral(s, ref i, "false", false);
            if (c == 'n') return ReadLiteral(s, ref i, "null", null);
            if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber(s, ref i);
            throw Error(s, i, $"unexpected '{c}'");
        }

        private static Dictionary<string, object?> ReadObject(string s, ref int i)
        {
            var result = new Dictionary<string, object?>();
            i++;
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == '}') { i++; return result; }
            while (true)
            {
                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != '"') throw Error(s, i, "expected a key");
                var key = ReadString(s, ref i);
                SkipWhitespace(s, ref i);
                if (i >= s.Length || s[i] != ':') throw Error(s, i, "expected ':'");
                i++;
                if (result.ContainsKey(key)) throw Error(s, i, $"duplicate key '{key}'");
                result[key] = ReadValue(s, ref i);
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == '}') { i++; return result; }
                throw Error(s, i, "expected ',' or '}'");
            }
        }

        private static List<object?> ReadArray(string s, ref int i)
        {
            var result = new List<object?>();
            i++;
            SkipWhitespace(s, ref i);
            if (i < s.Length && s[i] == ']') { i++; return result; }
            while (true)
            {
                result.Add(ReadValue(s, ref i));
                SkipWhitespace(s, ref i);
                if (i < s.Length && s[i] == ',') { i++; continue; }
                if (i < s.Length && s[i] == ']') { i++; return result; }
                throw Error(s, i, "expected ',' or ']'");
            }
        }

        private static string ReadString(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++;
            while (i < s.Length)
            {
                var c = s[i++];
                if (c == '"') return sb.ToString();
                if (c < 0x20) throw Error(s, i, "control character in string");
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                var e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw Error(s, i, "bad \\u escape");
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: throw Error(s, i, $"bad escape '\\{e}'");
                }
            }
            throw Error(s, i, "unterminated string");
        }

        private static double ReadNumber(string s, ref int i)
        {
            var start = i;
            if (s[i] == '-') i++;
            while (i < s.Length && "0123456789.eE+-".IndexOf(s[i]) >= 0) i++;
            var token = s.Substring(start, i - start);
            if (!double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                throw Error(s, start, $"bad number '{token}'");
            return value;
        }

        private static object? ReadLiteral(string s, ref int i, string literal, object? value)
        {
            if (string.CompareOrdinal(s, i, literal, 0, literal.Length) != 0) throw Error(s, i, $"expected {literal}");
            i += literal.Length;
            return value;
        }

        private static void SkipWhitespace(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r')) i++;
        }

        private static FormatException Error(string s, int i, string message) =>
            new FormatException($"JSON error at {i}: {message}");
    }
}
