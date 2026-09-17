using System;
using System.Linq;

namespace ClearMeasure.Bootcamp.UnitTests.BuildGates;

// Scratch probe for #9519: deliberately redundant `using System.Linq;` above,
// to prove Qodana no longer gates RedundantUsingDirective in src/UnitTests.
// Removed in a follow-up commit before merge.
internal static class Qodana9519ScratchProbe
{
    internal static Type MarkerType => typeof(Qodana9519ScratchProbe);
}
