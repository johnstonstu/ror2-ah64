using AH64.Modules;
using System;

internal static class LanguageFallbackChecks
{
    // Success means actual English values survive malformed, missing and partial loose files.
    public static void Run(Action<bool, string> Expect)
    {
        const string embedded = "{\"en\":{\"A\":\"baseline {0}\",\"B\":\"keep me\"}}";
        int warnings = 0;
        Action<string> warn = message => { Expect(!string.IsNullOrWhiteSpace(message), "fallback diagnostic context"); warnings++; };
        var missing = LanguageFallback.English(embedded, null, warn);
        Expect(missing["A"] == "baseline {0}" && missing["B"] == "keep me", "missing file preserves English");
        var malformed = LanguageFallback.English(embedded, "{broken", warn);
        Expect(malformed["A"] == "baseline {0}" && warnings == 1, "malformed file recovers and logs");
        var noEnglish = LanguageFallback.English(embedded, "{\"ru\":{\"A\":\"other\"}}", warn);
        Expect(noEnglish["B"] == "keep me" && warnings == 2, "missing en recovers and logs");
        var partial = LanguageFallback.English(embedded, "{\"en\":{\"A\":\"edited {0}\"}}", warn);
        Expect(partial["A"] == "edited {0}" && partial["B"] == "keep me" && warnings == 3, "partial en overlays without losing tokens");
        var empty = LanguageFallback.English(embedded, "{\"en\":{\"A\":\"\",\"B\":\"keep me\"}}", warn);
        Expect(empty["A"] == "baseline {0}" && warnings == 4, "empty English token retains baseline");
    }

}
