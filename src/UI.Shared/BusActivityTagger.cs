using System.Collections;
using System.Diagnostics;

namespace ClearMeasure.Bootcamp.UI.Shared;

internal static class BusActivityTagger
{
    public static void AddScalarPropertyTags(object message, Activity activity)
    {
        foreach (var property in message.GetType().GetProperties())
        {
            if (IsScalarProperty(property.PropertyType))
            {
                var propertyValue = property.GetValue(message);
                activity.SetTag($"bus.message.{property.Name}", propertyValue?.ToString() ?? string.Empty);
            }
        }
    }

    private static bool IsScalarProperty(Type propertyType) =>
        !typeof(IEnumerable).IsAssignableFrom(propertyType) || propertyType == typeof(string);
}
