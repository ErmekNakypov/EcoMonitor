using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EcoMonitor.Domain.Common;

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var type = value.GetType();
        var member = type.GetMember(value.ToString()).FirstOrDefault();
        if (member is null) return value.ToString();
        var display = member.GetCustomAttribute<DisplayAttribute>();
        return display?.Name ?? value.ToString();
    }
}
