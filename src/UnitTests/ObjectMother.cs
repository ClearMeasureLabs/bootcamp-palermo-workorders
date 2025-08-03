using System.Collections;
using System.Reflection;
using AutoBogus;
using AutoBogus.Conventions;
using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests;

public class ObjectMother
{
    private static bool _configured;
    public static object Lock = new();

    private static void EnsureConfigured()
    {
        if (!_configured)
        {
            lock (Lock)
            {
                if (!_configured)
                {
                    ConfigureBogus();
                    _configured = true;
                }
            }
        }
    }

    public static TK Faker<TK>()
    {
        EnsureConfigured();
        return AutoFaker.Generate<TK>();
    }

    private static void ConfigureBogus()
    {
        AutoFaker.Configure(builder =>
        {
            builder.WithConventions()
                .WithSkip<WorkOrder>(wo => wo.Creator)
                .WithSkip<WorkOrder>(wo => wo.Assignee)
                .WithSkip<Employee>(wo => wo.Roles)
                .WithOverride(new BogusOverrides());
        });
    }

    public static void AssertAllProperties(object expected, object actual)
    {
        if (expected.GetType().IsArray)
        {
            actual.ShouldBeEquivalentTo(expected);
            return;
        }

        var properties = expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var property in properties)
        {
            if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
            {
                continue;
            }

            var expectedValue = property.GetValue(expected, null);
            var actualValue = property.GetValue(actual, null);
            if (!Equals(expectedValue, actualValue))
            {
                if (property.DeclaringType != null)
                {
                    Assert.Fail(
                        $"Property {property.DeclaringType.Name}.{property.Name} does not match. Expected: {expectedValue} but was: {actualValue}");
                }
            }
        }
    }
}