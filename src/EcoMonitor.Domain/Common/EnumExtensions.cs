using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EcoMonitor.Domain.Common;

// Host-supplied hook so the Domain layer can return culture-aware display
// names without taking a direct dependency on Microsoft.Extensions.Localization.
// The Web layer sets Resolver at startup to a function that looks up
// "{EnumType}.{Value}" in EnumDisplayNames.resx. Unit tests / tools that
// never set Resolver get the English [Display(Name)] fallback unchanged.
public static class EnumDisplayLocalization
{
    public static Func<Enum, string?>? Resolver { get; set; }
}

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var localized = EnumDisplayLocalization.Resolver?.Invoke(value);
        if (!string.IsNullOrEmpty(localized)) return localized;

        var type = value.GetType();
        var member = type.GetMember(value.ToString()).FirstOrDefault();
        if (member is null) return value.ToString();
        var display = member.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? value.ToString();
    }
}
