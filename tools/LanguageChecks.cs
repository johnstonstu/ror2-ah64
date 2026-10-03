using AH64.Modules;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

internal static class LanguageChecks
{
    private static readonly string[] RequiredLanguages = { "en", "zh-CN", "ru", "pt-BR" };
    private static readonly Regex Placeholder = new Regex(@"\{(\d+)\}", RegexOptions.Compiled);
    private static readonly Regex Tag = new Regex(@"<style=[^>]+>|</style>|<color=[^>]+>|</color>", RegexOptions.Compiled);
    private static readonly Regex Suffix = new Regex(
        @"(?:AH64_PREFIX|prefix|tokenPrefix)\s*\+\s*""([A-Z0-9_]+)""",
        RegexOptions.Compiled);
    private static readonly Regex PaintName = new Regex(
        @"Name\s*=\s*""([A-Z]+)""",
        RegexOptions.Compiled);

    private static int failures;

    private static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: LanguageChecks <repo>");
            return 1;
        }

        string repo = args[0];
        SelfTestParser();
        CheckFile(repo);
        if (failures == 0)
        {
            Console.WriteLine("language check ok");
            return 0;
        }

        Console.Error.WriteLine(failures + " language check failure(s)");
        return 1;
    }

    private static void SelfTestParser()
    {
        const string sample = "{\"en\":{\"A\":\"line\\n<color=#CCD3E0>{0}</color>\",\"B\":\"quote\\\"s\"}}";
        var parsed = LanguageJson.Parse(sample);
        Expect(parsed["en"]["A"] == "line\n<color=#CCD3E0>{0}</color>", "newline and color escape");
        Expect(parsed["en"]["B"] == "quote\"s", "escaped quote");
        ExpectThrows("{\"en\":{\"A\":\"1\"},\"en\":{\"A\":\"2\"}}", "duplicate language");
        ExpectThrows("{\"en\":{\"A\":\"1\",\"A\":\"2\"}}", "duplicate token");
    }

    private static void ExpectThrows(string json, string label)
    {
        try
        {
            LanguageJson.Parse(json);
            Fail(label + " was accepted");
        }
        catch (FormatException)
        {
        }
    }

    private static void CheckFile(string repo)
    {
        string path = Path.Combine(repo, "AH64Mod", "Language", "AH64.language");
        if (!File.Exists(path))
        {
            Fail("missing " + path);
            return;
        }

        string text = File.ReadAllText(path);
        Dictionary<string, Dictionary<string, string>> languages;
        try
        {
            languages = LanguageJson.Parse(text);
        }
        catch (Exception ex)
        {
            Fail("AH64.language did not parse: " + ex.Message);
            return;
        }

        Dictionary<string, Dictionary<string, string>> reference;
        try
        {
            reference = ReadWithSystemTextJson(text);
        }
        catch (Exception ex)
        {
            Fail("System.Text.Json rejected AH64.language: " + ex.Message);
            return;
        }

        if (!Same(languages, reference))
            Fail("LanguageJson and System.Text.Json disagree on AH64.language");

        foreach (string code in RequiredLanguages)
        {
            if (!languages.ContainsKey(code))
                Fail("missing language " + code);
        }

        foreach (string code in languages.Keys)
        {
            if (Array.IndexOf(RequiredLanguages, code) < 0)
                Fail("unexpected language " + code);
        }

        if (!languages.ContainsKey("en"))
            return;

        var english = languages["en"];
        foreach (string code in languages.Keys)
        {
            var table = languages[code];
            foreach (string missing in english.Keys.Except(table.Keys))
                Fail(code + " is missing " + missing);
            foreach (string extra in table.Keys.Except(english.Keys))
                Fail(code + " has extra " + extra);

            foreach (string key in english.Keys)
            {
                if (!table.ContainsKey(key))
                    continue;
                string value = table[key];
                if (string.IsNullOrWhiteSpace(value))
                    Fail(code + " " + key + " is empty");
                string leftover = Placeholder.Replace(value, "");
                if (leftover.IndexOf('{') >= 0 || leftover.IndexOf('}') >= 0)
                    Fail(code + " " + key + " has a brace that is not a {0} placeholder");
                if (!SameMultiset(Placeholder.Matches(english[key]), Placeholder.Matches(value)))
                    Fail(code + " " + key + " placeholders differ from en");
                if (!SameMultiset(Tag.Matches(english[key]), Tag.Matches(value)))
                    Fail(code + " " + key + " style/color tags differ from en");
            }
        }

        foreach (string key in english.Keys)
        {
            var indexes = Placeholder.Matches(english[key]).Select(match => int.Parse(match.Groups[1].Value)).Distinct().OrderBy(n => n).ToArray();
            for (int i = 0; i < indexes.Length; i++)
            {
                if (indexes[i] != i)
                {
                    Fail(key + " placeholders must be contiguous from {0}");
                    break;
                }
            }
        }

        var referenced = ReferencedTokens(repo);
        var bound = BoundTokens(repo);
        foreach (string token in referenced)
        {
            if (!english.ContainsKey(token))
                Fail("code references " + token + " but en has no entry");
        }

        foreach (string token in bound)
        {
            if (!english.ContainsKey(token))
                Fail("Language.Bind references " + token + " but en has no entry");
        }

        foreach (string key in english.Keys)
        {
            bool hasPlaceholder = Placeholder.IsMatch(english[key]);
            if (hasPlaceholder && !bound.Contains(key))
                Fail(key + " has placeholders but is not passed to Language.Bind");
            if (!hasPlaceholder && bound.Contains(key))
                Fail(key + " is bound but its en text has no placeholders");
            if (!referenced.Contains(key) && !bound.Contains(key))
                Fail(key + " is in the language file but no C# token reference uses it");
        }
    }

    private static HashSet<string> ReferencedTokens(string repo)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        string mod = Path.Combine(repo, "AH64Mod");
        foreach (string file in Directory.GetFiles(mod, "*.cs", SearchOption.AllDirectories))
        {
            string source = File.ReadAllText(file);
            foreach (Match match in Suffix.Matches(source))
                tokens.Add("AH64_" + match.Groups[1].Value);
            if (file.EndsWith("AH64Skins.cs", StringComparison.Ordinal))
            {
                foreach (Match match in PaintName.Matches(source))
                    tokens.Add("AH64_" + match.Groups[1].Value + "_SKIN_NAME");
            }
            if (source.Contains("masteryAchievement"))
            {
                tokens.Add("ACHIEVEMENT_AH64_MASTERYACHIEVEMENT_NAME");
                tokens.Add("ACHIEVEMENT_AH64_MASTERYACHIEVEMENT_DESCRIPTION");
            }
        }

        // The logbook reads the body name token and replaces NAME with LORE
        // (AH64_NAME -> AH64_LORE). Nothing in this repo spells that suffix out.
        if (tokens.Contains("AH64_NAME"))
            tokens.Add("AH64_LORE");
        return tokens;
    }

    private static HashSet<string> BoundTokens(string repo)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        string source = File.ReadAllText(Path.Combine(repo, "AH64Mod", "Characters", "Survivors", "AH64", "Content", "AH64Tokens.cs"));
        foreach (Match match in Suffix.Matches(source))
        {
            if (NearbyBind(source, match.Index))
                tokens.Add("AH64_" + match.Groups[1].Value);
        }
        return tokens;
    }

    private static bool NearbyBind(string source, int index)
    {
        int start = Math.Max(0, index - 40);
        return source.IndexOf("Language.Bind", start, index - start, StringComparison.Ordinal) >= 0;
    }

    private static Dictionary<string, Dictionary<string, string>> ReadWithSystemTextJson(string text)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        using (JsonDocument document = JsonDocument.Parse(text))
        {
            foreach (JsonProperty language in document.RootElement.EnumerateObject())
            {
                var table = new Dictionary<string, string>(StringComparer.Ordinal);
                foreach (JsonProperty token in language.Value.EnumerateObject())
                    table.Add(token.Name, token.Value.GetString());
                result.Add(language.Name, table);
            }
        }
        return result;
    }

    private static bool Same(
        Dictionary<string, Dictionary<string, string>> left,
        Dictionary<string, Dictionary<string, string>> right)
    {
        if (left.Count != right.Count)
            return false;
        foreach (var language in left)
        {
            Dictionary<string, string> other;
            if (!right.TryGetValue(language.Key, out other) || other.Count != language.Value.Count)
                return false;
            foreach (var token in language.Value)
            {
                string value;
                if (!other.TryGetValue(token.Key, out value) || value != token.Value)
                    return false;
            }
        }
        return true;
    }

    private static bool SameMultiset(MatchCollection left, MatchCollection right)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (Match match in left)
        {
            if (!counts.ContainsKey(match.Value))
                counts[match.Value] = 0;
            counts[match.Value]++;
        }
        foreach (Match match in right)
        {
            if (!counts.ContainsKey(match.Value))
                return false;
            counts[match.Value]--;
            if (counts[match.Value] == 0)
                counts.Remove(match.Value);
        }
        return counts.Count == 0;
    }

    private static void Expect(bool condition, string label)
    {
        if (!condition)
            Fail(label);
    }

    private static void Fail(string message)
    {
        Console.Error.WriteLine("FAIL  " + message);
        failures++;
    }
}
