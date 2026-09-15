using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using Shouldly;

namespace ClearMeasure.Bootcamp.IntegrationTests.TestSupport;

[TestFixture]
public class LlmTestAttributeTests
{
    [Test]
    public void Constructor_WhenTryCountBelowOne_Throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new LlmTestAttribute(0));
    }

    [Test]
    public void Constructor_WhenDefault_UsesThreeTries()
    {
        new LlmTestAttribute().TryCount.ShouldBe(3);
    }

    [Test]
    public void Execute_WhenInnerPassesFirstTime_RunsOnceAndPasses()
    {
        var stub = new StubTestCommand(_ => ResultState.Success);

        var result = Run(stub, 3);

        result.ResultState.ShouldBe(ResultState.Success);
        stub.Executions.ShouldBe(1);
    }

    [Test]
    public void Execute_WhenInnerFailsThenPasses_RetriesAndPasses()
    {
        var stub = new StubTestCommand(attempt => attempt == 1 ? ResultState.Failure : ResultState.Success);

        var result = Run(stub, 3);

        result.ResultState.ShouldBe(ResultState.Success);
        stub.Executions.ShouldBe(2);
    }

    [Test]
    public void Execute_WhenInnerFailsEveryTime_ReportsWarningAfterAllTries()
    {
        var stub = new StubTestCommand(_ => ResultState.Failure, "assertion did not hold");

        var result = Run(stub, 3);

        result.ResultState.ShouldBe(ResultState.Warning);
        result.Message.ShouldContain("did not pass after 3 attempt(s)");
        result.Message.ShouldContain("assertion did not hold");
        stub.Executions.ShouldBe(3);
    }

    [Test]
    public void Execute_WhenInnerErrorsEveryTime_ReportsWarningAfterAllTries()
    {
        var stub = new StubTestCommand(_ => ResultState.Error, "boom");

        var result = Run(stub, 2);

        result.ResultState.ShouldBe(ResultState.Warning);
        stub.Executions.ShouldBe(2);
    }

    [Test]
    public void Execute_WhenInnerIsIgnored_StopsWithoutRetryAndKeepsIgnored()
    {
        var stub = new StubTestCommand(_ => ResultState.Ignored, "rate limited");

        var result = Run(stub, 3);

        result.ResultState.ShouldBe(ResultState.Ignored);
        stub.Executions.ShouldBe(1);
    }

    [Test]
    public void Execute_WhenInnerIsInconclusive_StopsWithoutRetryAndKeepsInconclusive()
    {
        var stub = new StubTestCommand(_ => ResultState.Inconclusive);

        var result = Run(stub, 3);

        result.ResultState.ShouldBe(ResultState.Inconclusive);
        stub.Executions.ShouldBe(1);
    }

    private static TestResult Run(StubTestCommand stub, int tryCount)
    {
        var context = new TestExecutionContext
        {
            CurrentTest = stub.Test,
            CurrentResult = stub.Test.MakeTestResult()
        };

        return new LlmTestAttribute(tryCount).Wrap(stub).Execute(context);
    }

    private sealed class StubTestCommand(Func<int, ResultState> outcome, string message = "")
        : TestCommand(new TestMethod(new MethodWrapper(typeof(Probe), nameof(Probe.Body))))
    {
        public int Executions { get; private set; }

        public override TestResult Execute(TestExecutionContext context)
        {
            Executions++;
            var result = Test.MakeTestResult();
            result.SetResult(outcome(Executions), message);
            return result;
        }
    }

    private sealed class Probe
    {
        public void Body()
        {
        }
    }
}
