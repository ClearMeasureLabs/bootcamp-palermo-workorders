using System.Collections;
using System.Diagnostics;
using System.Reflection;
using ClearMeasure.Bootcamp.Core;

namespace ClearMeasure.Bootcamp.UI.Shared;

internal static class BusActivityTagger
{
    public static void AddScalarPropertyTags(object message, Activity activity)
    {
        foreach (var property in message.GetType().GetProperties())
        {
            if (TryFormatScalarTag(message, property, out var tagValue))
            {
                activity.SetTag($"bus.message.{property.Name}", tagValue);
            }
        }
    }

    private static bool TryFormatScalarTag(object message, PropertyInfo property, out string tagValue)
    {
        tagValue = string.Empty;
        if (!IsScalarProperty(property.PropertyType)
            || property.GetCustomAttribute<ExcludeFromBusActivityAttribute>() != null)
        {
            return false;
        }

        var propertyValue = property.GetValue(message);
        if (propertyValue is null)
        {
            return false;
        }

        tagValue = propertyValue.ToString() ?? string.Empty;
        return true;
    }

    private static bool IsScalarProperty(Type propertyType) =>
        !typeof(IEnumerable).IsAssignableFrom(propertyType) || propertyType == typeof(string);
}
