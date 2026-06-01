using System.Globalization;

namespace EcoMonitor.Web.Helpers;

// Short, plain-language health advisories per AQI band. Conceptual basis:
// WHO Global Air Quality Guidelines 2021 — the strings here are not
// numeric claims, they paraphrase the standard categorical guidance
// ("sensitive groups should limit prolonged outdoor exertion" etc.) so the
// page can sit alongside the same AqiLevel coloring used by AqiHelper.
//
// Pattern mirrors AqiHelper: hardcoded strings per language (this project
// has not yet introduced .resx for sensor-side copy; AqiHelper.GetLabel uses
// the same hardcoded approach). The dispatcher picks Ru / En / Ky by the
// current UI culture's two-letter code; Ru is the default for the page.
public static class AirAdvisory
{
    public static string For(AqiLevel level, CultureInfo? culture = null)
    {
        var lang = (culture ?? CultureInfo.CurrentUICulture).TwoLetterISOLanguageName;
        return lang switch
        {
            "en" => InEnglish(level),
            "ky" => InKyrgyz(level),
            _ => InRussian(level)
        };
    }

    public static string InRussian(AqiLevel level) => level switch
    {
        AqiLevel.Good                  => "Качество воздуха хорошее — ограничений нет.",
        AqiLevel.Moderate              => "Качество воздуха умеренное. Людям с астмой и другими респираторными заболеваниями стоит сократить длительные нагрузки на улице.",
        AqiLevel.UnhealthyForSensitive => "Нездорово для чувствительных групп: детям, пожилым и людям с заболеваниями сердца или лёгких лучше ограничить пребывание на улице.",
        AqiLevel.Unhealthy             => "Нездоровый воздух. Сократите длительное пребывание и физическую активность на улице; чувствительным группам — оставаться дома.",
        AqiLevel.VeryUnhealthy         => "Очень нездоровый воздух. Избегайте активности на улице; держите окна закрытыми; используйте маску при выходе.",
        AqiLevel.Hazardous             => "Опасный уровень загрязнения. По возможности оставайтесь в помещении; чувствительным группам — не выходить на улицу.",
        _                              => "Данных недостаточно для рекомендации."
    };

    public static string InEnglish(AqiLevel level) => level switch
    {
        AqiLevel.Good                  => "Air quality is good — no restrictions.",
        AqiLevel.Moderate              => "Air quality is moderate. People with asthma or other respiratory conditions should reduce prolonged outdoor exertion.",
        AqiLevel.UnhealthyForSensitive => "Unhealthy for sensitive groups: children, the elderly and people with heart or lung disease should limit time outdoors.",
        AqiLevel.Unhealthy             => "Unhealthy air. Reduce prolonged outdoor activity; sensitive groups should stay indoors.",
        AqiLevel.VeryUnhealthy         => "Very unhealthy. Avoid outdoor activity; keep windows closed; consider a mask when outside.",
        AqiLevel.Hazardous             => "Hazardous pollution. Stay indoors if possible; sensitive groups should not go outside.",
        _                              => "Not enough data for a recommendation."
    };

    public static string InKyrgyz(AqiLevel level) => level switch
    {
        AqiLevel.Good                  => "Аба сапаты жакшы — чектөөлөр жок.",
        AqiLevel.Moderate              => "Аба сапаты орточо. Дем алуу оорулары бар адамдар көчөдө узак физикалык жүктөмдөн оолак болсун.",
        AqiLevel.UnhealthyForSensitive => "Сезимтал топторго зыяндуу: балдар, улгайгандар жана жүрөк/өпкө оорулуулар сыртта азыраак убакыт жүрсүн.",
        AqiLevel.Unhealthy             => "Зыяндуу аба. Көчөдөгү физикалык активдүүлүктү азайтыңыз; сезимтал топтор үйдө калсын.",
        AqiLevel.VeryUnhealthy         => "Абдан зыяндуу аба. Сыртта жүрбөңүз; терезелерди бекем жабыңыз; маска тагыныңыз.",
        AqiLevel.Hazardous             => "Кооптуу деңгээл. Мүмкүн болсо үйдө калыңыз; сезимтал топтор көчөгө чыкпасын.",
        _                              => "Сунуш үчүн маалымат жетишсиз."
    };
}
