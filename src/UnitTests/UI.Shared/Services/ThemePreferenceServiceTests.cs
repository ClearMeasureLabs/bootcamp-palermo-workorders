using ClearMeasure.Bootcamp.UI.Shared.Services;
using Microsoft.JSInterop;
using Shouldly;

namespace ClearMeasure.Bootcamp.UnitTests.UI.Shared.Services;

[TestFixture]
public class ThemePreferenceServiceTests
{
    [Test]
    public async Task InitializeAsync_ShouldReadStoredPreferenceFromInterop()
    {
        var js = new StubThemeJsRuntime { InitialTheme = "dark" };
        var sut = new ThemePreferenceService(js);

        await sut.InitializeAsync();

        sut.IsDarkMode.ShouldBeTrue();
    }

    [Test]
    public async Task SetDarkModeAsync_ShouldUpdateStateAndCallSetTheme()
    {
        var js = new StubThemeJsRuntime { InitialTheme = "light" };
        var sut = new ThemePreferenceService(js);
        await sut.InitializeAsync();

        await sut.SetDarkModeAsync(true);

        sut.IsDarkMode.ShouldBeTrue();
        js.SetThemeCalls.ShouldBe(1);
        js.LastSetThemeArg.ShouldNotBeNull();
        js.LastSetThemeArg!.Value.ShouldBeTrue();
    }

    [Test]
    public async Task InitializeAsync_ShouldReadCompactTablesPreferenceFromInterop()
    {
        var js = new StubThemeJsRuntime { InitialTheme = "light", InitialCompactTables = true };
        var sut = new ThemePreferenceService(js);

        await sut.InitializeAsync();

        sut.IsCompactTables.ShouldBeTrue();
    }

    [Test]
    public async Task SetCompactTablesAsync_ShouldUpdateStateAndCallSetCompactTables()
    {
        var js = new StubThemeJsRuntime { InitialTheme = "light" };
        var sut = new ThemePreferenceService(js);
        await sut.InitializeAsync();

        await sut.SetCompactTablesAsync(true);

        sut.IsCompactTables.ShouldBeTrue();
        js.SetCompactTablesCalls.ShouldBe(1);
        js.LastSetCompactTablesArg.ShouldNotBeNull();
        js.LastSetCompactTablesArg!.Value.ShouldBeTrue();
    }

    [Test]
    public async Task WhenJsInteropThrowsDisconnected_ShouldNotCrashOnInitialize()
    {
        var js = new StubDisconnectedJsRuntime();
        var sut = new ThemePreferenceService(js);

        await sut.InitializeAsync();

        sut.IsDarkMode.ShouldBeFalse();
    }

    private sealed class StubDisconnectedJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new JSDisconnectedException("circuit disconnected");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) =>
            throw new JSDisconnectedException("circuit disconnected");
    }

    private sealed class StubThemeJsRuntime : IJSRuntime
    {
        public string InitialTheme { get; init; } = "light";
        public bool InitialCompactTables { get; init; }
        public int SetThemeCalls { get; private set; }
        public bool? LastSetThemeArg { get; private set; }
        public int SetCompactTablesCalls { get; private set; }
        public bool? LastSetCompactTablesArg { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "import" && typeof(TValue) == typeof(IJSObjectReference))
            {
                var module = new StubThemeModule(InitialTheme, InitialCompactTables, this);
                return ValueTask.FromResult((TValue)(object)module);
            }

            throw new InvalidOperationException($"Unexpected InvokeAsync: {identifier}");
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
            object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);

        private sealed class StubThemeModule : IJSObjectReference
        {
            private readonly StubThemeJsRuntime _parent;
            private string _theme;
            private bool _compactTables;

            // ReSharper disable once ConvertToPrimaryConstructor -- epic guardrail: no mass primary-constructor conversion
            public StubThemeModule(string theme, bool compactTables, StubThemeJsRuntime parent)
            {
                _theme = theme;
                _compactTables = compactTables;
                _parent = parent;
            }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
            {
                if (identifier == "getTheme" && typeof(TValue) == typeof(string))
                    return ValueTask.FromResult((TValue)(object)_theme);

                if (identifier == "getCompactTables" && typeof(TValue) == typeof(bool))
                    return ValueTask.FromResult((TValue)(object)_compactTables);

                if (identifier == "syncDomFromTheme")
                {
                    _theme = (string)args![0]!;
                    return ValueTask.FromResult(default(TValue)!);
                }

                if (identifier == "setTheme")
                {
                    _parent.LastSetThemeArg = (bool)args![0]!;
                    _parent.SetThemeCalls++;
                    _theme = _parent.LastSetThemeArg.Value ? "dark" : "light";
                    return ValueTask.FromResult(default(TValue)!);
                }

                if (identifier == "setCompactTables")
                {
                    _parent.LastSetCompactTablesArg = (bool)args![0]!;
                    _parent.SetCompactTablesCalls++;
                    _compactTables = _parent.LastSetCompactTablesArg.Value;
                    return ValueTask.FromResult(default(TValue)!);
                }

                throw new InvalidOperationException($"Unexpected module InvokeAsync: {identifier}");
            }

            public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken,
                object?[]? args) =>
                InvokeAsync<TValue>(identifier, args);

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
