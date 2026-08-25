using ClearMeasure.Bootcamp.UI.Shared.Authentication;
using Microsoft.JSInterop;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Client.Authentication;

[TestFixture]
public class LocalStorageUserSessionStoreTests
{
    private const string StorageKey = "bootcamp.userSession.username";

    [Test]
    public async Task ClearAsync_AfterSet_ShouldYieldNull()
    {
        var js = new StubLocalStorageJsRuntime();
        var store = new LocalStorageUserSessionStore(js);
        await store.SetAsync("tlovejoy");

        await store.ClearAsync();

        (await store.GetAsync()).ShouldBeNullOrEmpty();
        js.RemoveItemCalls.ShouldBe(1);
        js.GetItemCalls.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Test]
    public async Task ClearAsync_ShouldThrow_WhenGetItemStillReturnsUsername()
    {
        var js = new StubLocalStorageJsRuntime { RefuseToRemove = true };
        var store = new LocalStorageUserSessionStore(js);
        await store.SetAsync("tlovejoy");

        var ex = await Should.ThrowAsync<InvalidOperationException>(store.ClearAsync);

        ex.Message.ShouldContain(StorageKey);
        (await store.GetAsync()).ShouldBe("tlovejoy");
    }

    private sealed class StubLocalStorageJsRuntime : IJSRuntime
    {
        private string? _value;

        public bool RefuseToRemove { get; init; }
        public int RemoveItemCalls { get; private set; }
        public int GetItemCalls { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args)
        {
            switch (identifier)
            {
                case "localStorage.getItem":
                    GetItemCalls++;
                    return ValueTask.FromResult((TValue)(object?)_value!);
                case "localStorage.setItem":
                    _value = (string?)args![1];
                    return ValueTask.FromResult(default(TValue)!);
                case "localStorage.removeItem":
                    RemoveItemCalls++;
                    if (!RefuseToRemove)
                    {
                        _value = null;
                    }

                    return ValueTask.FromResult(default(TValue)!);
                default:
                    throw new InvalidOperationException($"Unexpected InvokeAsync: {identifier}");
            }
        }
    }
}
