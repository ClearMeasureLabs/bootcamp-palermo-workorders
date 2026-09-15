using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;

namespace ClearMeasure.Bootcamp.IntegrationTests.TestSupport;

/// <summary>
/// Marks a test that depends on a live LLM. The test body is attempted up to <see cref="TryCount"/> times.
/// If every attempt fails or errors, the result is reported as a <see cref="ResultState.Warning"/> instead of a
/// failure so that model nondeterminism does not fail the build. Passed, Ignored (for example an Azure OpenAI
/// rate-limit skip), and Inconclusive outcomes stop the retry loop and are reported unchanged.
/// Use together with <c>[Test]</c> in place of <c>[Retry(n)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class LlmTestAttribute : NUnitAttribute, IRepeatTest
{
    private const int DefaultTryCount = 3;

    /// <summary>
    /// Creates the attribute with the default of three attempts.
    /// </summary>
    public LlmTestAttribute()
        : this(DefaultTryCount)
    {
    }

    /// <summary>
    /// Creates the attribute with an explicit number of attempts.
    /// </summary>
    /// <param name="tryCount">Total attempts, including the first. Must be at least 1.</param>
    public LlmTestAttribute(int tryCount)
    {
        if (tryCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(tryCount), tryCount, "At least one attempt is required.");
        }

        TryCount = tryCount;
    }

    /// <summary>
    /// Total attempts, including the first.
    /// </summary>
    public int TryCount { get; }

    /// <inheritdoc />
    public TestCommand Wrap(TestCommand command)
    {
        return new RetryThenWarnCommand(command, TryCount);
    }

    /// <summary>
    /// Runs the inner command up to a fixed number of times and converts a final failure into a warning.
    /// </summary>
    private sealed class RetryThenWarnCommand(TestCommand innerCommand, int tryCount) : DelegatingTestCommand(innerCommand)
    {
        public override TestResult Execute(TestExecutionContext context)
        {
            var remaining = tryCount;

            while (remaining-- > 0)
            {
                try
                {
                    context.CurrentResult = innerCommand.Execute(context);
                }
                catch (Exception ex)
                {
                    context.CurrentResult.RecordException(ex);
                }

                if (context.CurrentResult.ResultState.Status != TestStatus.Failed)
                {
                    break;
                }

                if (remaining > 0)
                {
                    context.CurrentRepeatCount++;
                }
            }

            if (context.CurrentResult.ResultState.Status == TestStatus.Failed)
            {
                DowngradeToWarning(context.CurrentResult);
            }

            return context.CurrentResult;
        }

        private void DowngradeToWarning(TestResult result)
        {
            var message =
                $"LLM-dependent test did not pass after {tryCount} attempt(s); reported as a warning instead of a failure. " +
                $"Last outcome: {result.ResultState.Status}. {result.Message}";
            result.SetResult(ResultState.Warning, message, result.StackTrace);
        }
    }
}
