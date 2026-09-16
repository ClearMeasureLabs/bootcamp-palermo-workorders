using System.Collections;
using System.Reflection;
using AutoBogus;
using AutoBogus.Conventions;
using ClearMeasure.Bootcamp.Core.Model;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests;

// ReSharper disable once ClassNeverInstantiated.Global -- instantiated by test infrastructure (NUnit / reflection)
public class ObjectMother
{
    private static readonly Lazy<bool> Configured = new(() =>
    {
        ConfigureBogus();
        return true;
    });

    private static void EnsureConfigured()
    {
        _ = Configured.Value;
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
                .WithSkip<WorkOrder>(wo => wo.DueDate)
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
            AssertComparableProperty(property, expected, actual);
        }
    }

    private static void AssertComparableProperty(PropertyInfo property, object expected, object actual)
    {
        if (IsNonStringEnumerable(property.PropertyType))
        {
            return;
        }

        var expectedValue = property.GetValue(expected, null);
        var actualValue = property.GetValue(actual, null);
        if (Equals(expectedValue, actualValue))
        {
            return;
        }

        FailMismatchedProperty(property, expectedValue, actualValue);
    }

    private static bool IsNonStringEnumerable(Type propertyType)
    {
        return typeof(IEnumerable).IsAssignableFrom(propertyType) && propertyType != typeof(string);
    }

    private static void FailMismatchedProperty(PropertyInfo property, object? expectedValue, object? actualValue)
    {
        if (property.DeclaringType == null)
        {
            return;
        }

        Assert.Fail(
            $"Property {property.DeclaringType.Name}.{property.Name} does not match. Expected: {expectedValue} but was: {actualValue}");
    }
}