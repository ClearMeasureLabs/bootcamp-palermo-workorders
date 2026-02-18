using System.Text.Json;
using ClearMeasure.Bootcamp.Core;
using ClearMeasure.Bootcamp.Core.Model;
using ClearMeasure.Bootcamp.Core.Model.StateCommands;
using ClearMeasure.Bootcamp.Core.Queries;
using ClearMeasure.Bootcamp.Core.Services;
using ClearMeasure.Bootcamp.UI.Client;
using ClearMeasure.Bootcamp.UI.Client.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.Core.Queries;

public class RemotableRequestTests
{
    [Test]
    public void ShouldSerialize()
    {
        AssertRemotable(new ForecastQuery());
        AssertRemotable(new WeatherForecast[1] { ObjectMother.Faker<WeatherForecast>() });
        AssertRemotable(new HealthCheckRemotableRequest());
        AssertRemotable(HealthStatus.Degraded);
        AssertRemotable(ObjectMother.Faker<WorkOrderSpecificationQuery>());
        AssertRemotable(WorkOrderStatus.Draft);
    }

    [Test]
    public void ShouldBeRemotableCompatible()
    {
        var order = ObjectMother.Faker<WorkOrder>();
        AssertRemotable(order);
        AssertRemotable(new ServerHealthCheckQuery());
        AssertRemotable(ObjectMother.Faker<Role>());

        var employee = ObjectMother.Faker<Employee>();
        var role = ObjectMother.Faker<Role>();
        employee.AddRole(role);
        var rehydratedRole = ((Employee)AssertRemotable(employee)).Roles.Single(role1 => role1 == role);
        ObjectMother.AssertAllProperties(role, rehydratedRole);

        AssertRemotable(new SaveDraftCommand(order, employee));
        AssertRemotable(new StateCommandResult(order, "Save", "message"));
    }

    [Test]
    public void ShouldSerializeStateCommand()
    {
        var order = ObjectMother.Faker<WorkOrder>();
        var employee = ObjectMother.Faker<Employee>();
        IStateCommand command = new SaveDraftCommand(order, employee);
        AssertRemotable(command);
    }

    public static object AssertRemotable(object theObject)
    {
        var rehydratedQuery = SimulateRemoteObject(theObject);

        ObjectMother.AssertAllProperties(theObject, rehydratedQuery);
        rehydratedQuery.ShouldBe(theObject);
        return rehydratedQuery;
    }

    public static object SimulateRemoteObject(object theObject)
    {
        var json = new WebServiceMessage(theObject).GetJson();
        var message = JsonSerializer.Deserialize<WebServiceMessage>(json);
        var rehydratedQuery = message!.GetBodyObject();
        return rehydratedQuery;
    }

    public static T SimulateRemoteObject<T>(T theObject)
    {
        return (T)SimulateRemoteObject((object)theObject! ?? throw new InvalidOperationException());
    }
}