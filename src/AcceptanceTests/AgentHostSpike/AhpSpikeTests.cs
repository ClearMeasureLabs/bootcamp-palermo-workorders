using System.Globalization;
using ClearMeasure.Bootcamp.UI.Client.Pages;

namespace ClearMeasure.Bootcamp.AcceptanceTests.AgentHostSpike;

/// <summary>
/// Spike S1 evidence test (#10943): the Agent Host Protocol .NET client connects to the in-app
/// test host at /spike/ahp, streams 1,000 chat/delta actions, survives a simulated network drop,
/// and replays missed actions on reconnect so the render is complete with zero gaps.
/// </summary>
[TestFixture]
public class AhpSpikeTests : AcceptanceTestBase
{
    /// <summary>
    /// Spike S1 evidence test (#10943): renders all 1,000 streamed chat/delta actions after a
    /// mid-stream connection drop, with the replay covering the gap so Gaps stays zero and
    /// LastServerSeq advances past the sequence recorded at drop time.
    /// </summary>
    [Test, Retry(2)]
    public async Task Should_RenderThousandDeltasAndReplayMissedActions_When_ConnectionDropsMidStream()
    {
        await LoginAsCurrentUser();

        await Page.GotoAsync("/spike/ahp");
        await Expect(TestId(AhpSpike.Elements.ConnectButton)).ToBeVisibleAsync(new() { Timeout = 90_000 });

        await Click(nameof(AhpSpike.Elements.ConnectButton));
        await Expect(TestId(AhpSpike.Elements.ConnectionStatus)).ToHaveTextAsync("Connected", new() { Timeout = 30_000 });
        await Expect(TestId(AhpSpike.Elements.ServerUser)).ToContainTextAsync(CurrentUser.UserName);

        await Click(nameof(AhpSpike.Elements.StartButton));
        await WaitForNumberAsync(AhpSpike.Elements.DeltaActionsReceived, n => n >= 250, 90_000);

        await Click(nameof(AhpSpike.Elements.DropButton));
        await Expect(TestId(AhpSpike.Elements.ConnectionStatus)).ToHaveTextAsync("Disconnected", new() { Timeout = 10_000 });
        var seqAtDrop = await ReadNumberAsync(AhpSpike.Elements.LastServerSeq);
        await Task.Delay(1500);

        await Click(nameof(AhpSpike.Elements.ReconnectButton));
        await Expect(TestId(AhpSpike.Elements.ConnectionStatus)).ToHaveTextAsync("Connected", new() { Timeout = 30_000 });
        await WaitForNumberAsync(AhpSpike.Elements.Replayed, n => n > 0, 30_000);

        await Expect(TestId(AhpSpike.Elements.TurnStatus)).ToHaveTextAsync("complete", new() { Timeout = 90_000 });

        (await ReadNumberAsync(AhpSpike.Elements.DeltasRendered)).ShouldBe(1000);
        (await ReadNumberAsync(AhpSpike.Elements.DeltaActionsReceived)).ShouldBe(1000);
        (await ReadNumberAsync(AhpSpike.Elements.Gaps)).ShouldBe(0);
        (await ReadNumberAsync(AhpSpike.Elements.LastServerSeq)).ShouldBeGreaterThan(seqAtDrop);
        (await ReadNumberAsync(AhpSpike.Elements.Replayed)).ShouldBeGreaterThan(0);

        await TakeScreenshotAsync(1, "AhpSpikeComplete");
    }

    private ILocator TestId(AhpSpike.Elements element) => Page.GetByTestId(Enum.GetName(element)!);

    private async Task<long> ReadNumberAsync(AhpSpike.Elements element)
    {
        var text = await TestId(element).InnerTextAsync();
        return long.Parse(text.Trim(), CultureInfo.InvariantCulture);
    }

    private async Task WaitForNumberAsync(AhpSpike.Elements element, Func<long, bool> condition, int timeoutMs)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        long lastValue = 0;
        while (true)
        {
            var text = (await TestId(element).InnerTextAsync()).Trim();
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            {
                lastValue = value;
                if (condition(value))
                {
                    return;
                }
            }

            if (Environment.TickCount64 >= deadline)
            {
                false.ShouldBeTrue($"{element} should satisfy the condition after {timeoutMs}ms, but was '{text}' (last parsed {lastValue})");
            }

            await Task.Delay(100);
        }
    }
}
