using System;
using System.Collections.Generic;

namespace AH64.Modules
{
    internal static class LanguageFallback
    {
        public static Dictionary<string, string> English(string embeddedJson, string looseJson, Action<string> warn)
        {
            var embedded = LanguageJson.Parse(embeddedJson);
            Dictionary<string, string> baseline;
            if (!embedded.TryGetValue("en", out baseline) || baseline.Count == 0)
                throw new FormatException("Embedded AH64.language has no English baseline.");
            var result = new Dictionary<string, string>(baseline, StringComparer.Ordinal);
            if (looseJson == null) return result;

            Dictionary<string, Dictionary<string, string>> loose;
            try { loose = LanguageJson.Parse(looseJson); }
            catch (FormatException error) {
                warn("Loose file is invalid; keeping embedded English: " + error.Message);
                return result;
            }
            Dictionary<string, string> english;
            if (!loose.TryGetValue("en", out english)) {
                warn("Loose file has no en section; keeping embedded English.");
                return result;
            }
            int retained = 0;
            foreach (var pair in baseline) {
                string value;
                if (english.TryGetValue(pair.Key, out value) && !string.IsNullOrWhiteSpace(value))
                    result[pair.Key] = value;
                else retained++;
            }
            if (retained > 0) warn("Loose en section is incomplete; retaining " + retained + " embedded English tokens.");
            return result;
        }
    }
}
