using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EcoMonitor.Infrastructure.Telegram;

public sealed class BotLocalizer
{
    public const string DefaultLanguage = "ru";
    private static readonly string[] SupportedLanguages = { "ru", "ky", "en" };

    private readonly Dictionary<string, Dictionary<string, string>> _strings = new();
    private readonly ILogger<BotLocalizer> _logger;

    public BotLocalizer(ILogger<BotLocalizer> logger)
    {
        _logger = logger;
        LoadAll();
    }

    public static IReadOnlyList<string> Languages => SupportedLanguages;

    public static bool IsSupported(string? language) =>
        !string.IsNullOrEmpty(language) && SupportedLanguages.Contains(language);

    public string Get(string? language, string key, params object[] formatArgs)
    {
        var lang = IsSupported(language) ? language! : DefaultLanguage;

        if (_strings.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
        {
            return Format(value, formatArgs);
        }

        if (lang != "en" && _strings.TryGetValue("en", out var enDict) && enDict.TryGetValue(key, out var enValue))
        {
            return Format(enValue, formatArgs);
        }

        _logger.LogWarning("Bot string missing for key {Key} (language {Lang})", key, lang);
        return $"[{key}]";
    }

    private static string Format(string template, object[] args) =>
        args.Length == 0 ? template : string.Format(CultureInfo.InvariantCulture, template, args);

    private void LoadAll()
    {
        foreach (var lang in SupportedLanguages)
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "Telegram", "Localization", $"bot-strings.{lang}.json");
                if (!File.Exists(path))
                {
                    _logger.LogWarning("Bot string file missing for language {Lang}: {Path}", lang, path);
                    _strings[lang] = new Dictionary<string, string>();
                    continue;
                }

                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
                _strings[lang] = parsed;
                _logger.LogInformation("Loaded {Count} bot strings for language {Lang}", parsed.Count, lang);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load bot strings for language {Lang}", lang);
                _strings[lang] = new Dictionary<string, string>();
            }
        }
    }
}
