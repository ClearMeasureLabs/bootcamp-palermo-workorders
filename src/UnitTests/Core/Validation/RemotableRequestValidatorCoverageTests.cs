using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Validation;
using ClearMeasure.Bootcamp.UI.Server.Validation;
using FluentValidation;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Validation;

[TestFixture]
public class RemotableRequestValidatorCoverageTests
{
    [Test]
    public void ShouldHaveValidatorForEveryRemotableRequestInCore()
    {
        var coreAssembly = typeof(IRemotableRequest).Assembly;
        var validatorAssemblies = new[]
        {
            typeof(WebServiceMessageValidator).Assembly,
            typeof(EmployeeGetAllQueryValidator).Assembly
        };

        var validatedTypes = validatorAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type is { IsAbstract: false, IsInterface: false })
            .SelectMany(type => type.GetInterfaces())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
            .Select(i => i.GetGenericArguments()[0])
            .ToHashSet();

        var remotableRequests = coreAssembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                           && typeof(IRemotableRequest).IsAssignableFrom(type))
            .ToArray();

        var missing = remotableRequests
            .Where(type => !validatedTypes.Contains(type))
            .Select(type => type.FullName)
            .OrderBy(name => name)
            .ToArray();

        remotableRequests.ShouldNotBeEmpty();
        missing.ShouldBeEmpty(
            "Remotable requests without an IValidator<T>: " + string.Join(", ", missing));
    }
}
