using R2API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace AH64.Modules
{
    /// <summary>
    /// Ships <c>AH64.language</c> beside the DLL. R2API.Language loads every
    /// <c>*.language</c> file under BepInEx/plugins and prefers a language-specific
    /// entry over the generic fallback from <see cref="LanguageAPI.Add"/>.
    /// Tokens that include numbers stay as <c>{0}</c> templates in the file; this
    /// class fills them when the game asks for the string. Skill numbers come from
    /// <c>AH64StaticValues</c>. The descend sentence also reads the altitude config.
    /// </summary>
    internal static class Language
    {
        public const string FileName = "AH64.language";
        private const string EmbeddedName = "AH64.AH64.language";

        private static readonly Dictionary<string, string> English =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private static readonly Dictionary<string, Func<CultureInfo, object[]>> Formatters =
            new Dictionary<string, Func<CultureInfo, object[]>>(StringComparer.Ordinal);
        private static readonly HashSet<string> LoggedFailures =
            new HashSet<string>(StringComparer.Ordinal);
        private static bool hookInstalled;

        public static void Init()
        {
            Dictionary<string, string> english;
            try
            {
                string embedded = ReadEmbeddedLanguageFile();
                foreach (var pair in LanguageFallback.English(embedded, null, Log.Warning))
                    English[pair.Key] = pair.Value;
                english = LanguageFallback.English(embedded, ReadLooseLanguageFile(), Log.Warning);
            }
            catch (Exception error)
            {
                Log.Error("Cannot register embedded AH-64 English fallback: " + error);
                throw;
            }

            // Register all embedded tokens before installing the formatting wrapper.
            // Loose English edits can override values but cannot remove fallback keys.
            foreach (var pair in english) LanguageAPI.Add(pair.Key, pair.Value);

            InstallHook();
        }

        /// <summary>
        /// The file stores a template for <paramref name="token"/>. The delegate supplies the
        /// values, already formatted for the active language's number style.
        /// </summary>
        public static void Bind(string token, Func<CultureInfo, object[]> arguments)
        {
            Formatters[token] = arguments;
        }

        private static void InstallHook()
        {
            if (hookInstalled)
                return;
            // LanguageAPI.Add above has already installed R2API's hook. Ours must wrap it,
            // so the template R2API returns is what gets formatted.
            hookInstalled = true;
            On.RoR2.Language.GetLocalizedStringByToken += FormatToken;
        }

        private static string FormatToken(On.RoR2.Language.orig_GetLocalizedStringByToken orig, RoR2.Language self, string token)
        {
            string raw = orig(self, token);
            string fallback;
            if (token != null && string.IsNullOrWhiteSpace(raw) && English.TryGetValue(token, out fallback))
            {
                LogFailure(token, "Empty localized value; using embedded English");
                raw = fallback;
            }
            if (token == null || !Formatters.ContainsKey(token))
                return raw;

            CultureInfo culture = CultureFor(self != null ? self.name : null);
            object[] args;
            try
            {
                args = Formatters[token](culture);
            }
            catch (Exception ex)
            {
                LogFailure(token, ex.Message);
                return raw;
            }

            return ApplyTemplate(token, raw, culture, args);
        }

        private static string ApplyTemplate(string token, string template, CultureInfo culture, object[] args)
        {
            try
            {
                return string.Format(culture, template, args);
            }
            catch (FormatException ex)
            {
                LogFailure(token, ex.Message);
                string english;
                if (English.TryGetValue(token, out english) && !string.Equals(template, english, StringComparison.Ordinal))
                {
                    try
                    {
                        return string.Format(CultureInfo.InvariantCulture, english, args);
                    }
                    catch (FormatException fallbackError)
                    {
                        LogFailure(token + ":embedded-en", fallbackError.Message);
                    }
                }
                return template;
            }
        }

        private static void LogFailure(string token, string message)
        {
            if (!LoggedFailures.Add(token))
                return;
            Log.Error("Could not fill language token " + token + ": " + message);
        }

        private static CultureInfo CultureFor(string languageName)
        {
            if (string.IsNullOrEmpty(languageName))
                return CultureInfo.InvariantCulture;
            // RoR2 names the Russian language "RU" and Brazilian Portuguese "pt-BR".
            // Only those two need a decimal comma; everything else keeps the invariant dot
            // the English tooltips were written with.
            try
            {
                // A runtime without those cultures (invariant globalization) keeps the
                // invariant dot instead of throwing out of the language hook.
                if (languageName.Equals("ru", StringComparison.OrdinalIgnoreCase))
                    return CultureInfo.GetCultureInfo("ru");
                if (languageName.Equals("pt-BR", StringComparison.OrdinalIgnoreCase))
                    return CultureInfo.GetCultureInfo("pt-BR");
            }
            catch (CultureNotFoundException error)
            {
                LogFailure("culture:" + languageName, error.Message + "; using invariant numbers");
            }
            return CultureInfo.InvariantCulture;
        }

        private static string ReadLooseLanguageFile()
        {
            try
            {
                string location = AH64Plugin.instance.Info.Location;
                string path = Path.Combine(Path.GetDirectoryName(location), FileName);
                if (File.Exists(path))
                    return File.ReadAllText(path);
                Log.Warning("AH64.language is not beside the DLL at " + path + ". Using the copy embedded in the assembly. Translations ship only with the loose file.");
            }
            catch (Exception ex)
            {
                Log.Warning("Could not read AH64.language beside the DLL: " + ex.Message);
            }

            return null;
        }

        private static string ReadEmbeddedLanguageFile()
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedName);
            if (stream == null)
                throw new InvalidOperationException("Missing embedded resource " + EmbeddedName);
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }
    }
}
