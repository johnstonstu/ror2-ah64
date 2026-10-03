using System;
using System.Collections.Generic;
using System.Text;

namespace AH64.Modules
{
    /// <summary>
    /// Parser for the one shape R2API.Language accepts: a JSON object whose values are
    /// objects of strings, keyed by language code. Kept local so a bad file fails here
    /// with a token name instead of inside the game's loader.
    /// </summary>
    internal static class LanguageJson
    {
        public static Dictionary<string, Dictionary<string, string>> Parse(string text)
        {
            if (text == null)
                throw new FormatException("Language file is empty.");
            if (text.Length > 0 && text[0] == '\uFEFF')
                text = text.Substring(1);

            var parser = new Parser(text);
            Dictionary<string, Dictionary<string, string>> root = parser.ParseLanguages();
            parser.SkipWs();
            if (!parser.End)
                throw new FormatException("Unexpected trailing data in the language file at " + parser.Position + ".");
            return root;
        }

        private sealed class Parser
        {
            private readonly string text;
            private int index;

            public Parser(string text)
            {
                this.text = text;
            }

            public int Position => index;
            public bool End => index >= text.Length;

            public void SkipWs()
            {
                while (index < text.Length)
                {
                    char c = text[index];
                    if (c != ' ' && c != '\t' && c != '\r' && c != '\n')
                        break;
                    index++;
                }
            }

            public Dictionary<string, Dictionary<string, string>> ParseLanguages()
            {
                var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
                Expect('{');
                SkipWs();
                if (Consume('}'))
                    return result;

                while (true)
                {
                    string language = ParseString();
                    SkipWs();
                    Expect(':');
                    SkipWs();
                    if (result.ContainsKey(language))
                        throw new FormatException("Duplicate language '" + language + "'.");
                    result.Add(language, ParseStrings());
                    SkipWs();
                    if (Consume('}'))
                        break;
                    Expect(',');
                    SkipWs();
                }

                return result;
            }

            public Dictionary<string, string> ParseStrings()
            {
                var result = new Dictionary<string, string>(StringComparer.Ordinal);
                Expect('{');
                SkipWs();
                if (Consume('}'))
                    return result;

                while (true)
                {
                    string key = ParseString();
                    SkipWs();
                    Expect(':');
                    SkipWs();
                    if (result.ContainsKey(key))
                        throw new FormatException("Duplicate token '" + key + "'.");
                    result.Add(key, ParseString());
                    SkipWs();
                    if (Consume('}'))
                        break;
                    Expect(',');
                    SkipWs();
                }

                return result;
            }

            public string ParseString()
            {
                Expect('"');
                var builder = new StringBuilder();
                while (index < text.Length)
                {
                    char c = text[index++];
                    if (c == '"')
                        return builder.ToString();
                    if (c == '\\')
                    {
                        if (index >= text.Length)
                            throw new FormatException("Truncated escape.");
                        char escaped = text[index++];
                        switch (escaped)
                        {
                            case '"':
                            case '\\':
                            case '/':
                                builder.Append(escaped);
                                break;
                            case 'b':
                                builder.Append('\b');
                                break;
                            case 'f':
                                builder.Append('\f');
                                break;
                            case 'n':
                                builder.Append('\n');
                                break;
                            case 'r':
                                builder.Append('\r');
                                break;
                            case 't':
                                builder.Append('\t');
                                break;
                            case 'u':
                                builder.Append(ParseHexChar());
                                break;
                            default:
                                throw new FormatException("Unknown escape '\\" + escaped + "'.");
                        }
                    }
                    else if (c < ' ')
                    {
                        throw new FormatException("Raw control character in a string.");
                    }
                    else
                    {
                        builder.Append(c);
                    }
                }

                throw new FormatException("Unterminated string.");
            }

            private char ParseHexChar()
            {
                if (index + 4 > text.Length)
                    throw new FormatException("Truncated unicode escape.");
                int code = 0;
                for (int i = 0; i < 4; i++)
                    code = (code << 4) + HexValue(text[index++]);
                return (char)code;
            }

            private static int HexValue(char c)
            {
                if (c >= '0' && c <= '9')
                    return c - '0';
                if (c >= 'a' && c <= 'f')
                    return c - 'a' + 10;
                if (c >= 'A' && c <= 'F')
                    return c - 'A' + 10;
                throw new FormatException("Invalid hex digit '" + c + "'.");
            }

            private void Expect(char expected)
            {
                if (index >= text.Length || text[index] != expected)
                    throw new FormatException("Expected '" + expected + "' at " + index + ".");
                index++;
            }

            private bool Consume(char expected)
            {
                if (index < text.Length && text[index] == expected)
                {
                    index++;
                    return true;
                }
                return false;
            }
        }
    }
}
