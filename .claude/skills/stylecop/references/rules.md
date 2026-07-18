# StyleCop rule catalog — pragmatic clean-architecture profile

Source of truth for the `stylecop` skill. Three buckets: **Enforced (warning)**,
**Documentation (suggestion)**, **Disabled (ignore)**. Report only Enforced and
Documentation findings. Column numbers are best-effort.

---

## Disabled — never report these

| Rule | Name | Why disabled |
|------|------|--------------|
| SA1101 | PrefixLocalCallsWithThis | Modern C# does not use `this.` for local members. |
| SA1309 | FieldNamesMustNotBeginWithUnderscore | `_camelCase` private fields are the house convention. |
| SA1633 | FileMustHaveHeader | No file header comments required. |
| SA1642 | ConstructorSummaryDocumentationMustBeginWithStandardText | Boilerplate summary text not required. |
| SA1643 | DestructorSummaryDocumentationMustBeginWithStandardText | Boilerplate summary text not required. |
| SA1413 | UseTrailingCommaInMultiLineInitializers | Trailing commas optional. |

Also ignore: SA1200 violations that are only about placing usings *inside* the
namespace — this profile wants usings **outside** the namespace (see SA1200 below).

---

## Documentation — report as `suggestion`, public API only

Applies to **public / exposed** members only. Do **not** flag missing docs on
`private`, `internal`, or `protected internal` members, or on private fields.

| Rule | Name | Message |
|------|------|---------|
| SA1600 | ElementsMustBeDocumented | Elements should be documented. |
| SA1601 | PartialElementsMustBeDocumented | Partial elements should be documented. |
| SA1602 | EnumerationItemsMustBeDocumented | Enumeration items should be documented. |

Non-compliant (public, undocumented):

```csharp
public sealed class WorkRequest
{
    public string Number { get; set; }   // SA1600 (suggestion): public, no XML doc
}
```

Compliant:

```csharp
/// <summary>Represents a unit of maintenance work.</summary>
public sealed class WorkRequest
{
    /// <summary>Gets or sets the work request number.</summary>
    public string Number { get; set; }
}
```

Internal/private members need no docs:

```csharp
internal void Recalculate() { }   // OK — not flagged
private int _retryCount;          // OK — not flagged
```

---

## Enforced — report as `warning`

### Using directives

| Rule | Name | Message |
|------|------|---------|
| SA1200 | UsingDirectivesMustBePlacedCorrectly | Using directive should appear outside a namespace declaration. |
| SA1208 | SystemUsingDirectivesMustBePlacedBeforeOtherUsingDirectives | Using directive for 'System.*' should appear before other using directives. |
| SA1210 | UsingDirectivesMustBeOrderedAlphabeticallyByNamespace | Using directives should be ordered alphabetically by namespace. |

Non-compliant:

```csharp
namespace ClearMeasure.Bootcamp.Core
{
    using Microsoft.Extensions.Logging;   // SA1200: usings inside namespace
    using System;                         // SA1208: System after non-System
}
```

Compliant (usings outside namespace, System first, alphabetical):

```csharp
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ClearMeasure.Bootcamp.Core;
```

### Element ordering

| Rule | Name | Message |
|------|------|---------|
| SA1201 | ElementsMustAppearInTheCorrectOrder | A {member} should not follow a {member}. |
| SA1202 | ElementsMustBeOrderedByAccess | 'public' members should appear before 'private' members. |
| SA1203 | ConstantsMustAppearBeforeFields | Constants should appear before fields. |
| SA1204 | StaticElementsMustAppearBeforeInstanceElements | Static members should appear before non-static members. |

Canonical order within a type: fields → constructors → properties → methods; and
within each group: `public` → `internal` → `protected` → `private`, `const` before
non-const, `static` before instance.

Non-compliant:

```csharp
public class Employee
{
    private void Save() { }        // SA1201: method before property
    public string Name { get; }    // SA1202: public after private method

    private int _id;
    private const int Max = 10;    // SA1203: const after field
}
```

### Layout & readability

| Rule | Name | Message |
|------|------|---------|
| SA1028 | CodeMustNotContainTrailingWhitespace | Code should not contain trailing whitespace. |
| SA1122 | UseStringEmptyForEmptyStrings | Use string.Empty for empty strings. |
| SA1400 | AccessModifierMustBeDeclared | Element should declare an access modifier. |
| SA1401 | FieldsMustBePrivate | Field should be private. |
| SA1503 | BracesMustNotBeOmitted | Braces should not be omitted. |
| SA1516 | ElementsMustBeSeparatedByBlankLine | Elements should be separated by a blank line. |

Non-compliant:

```csharp
class Service               // SA1400: no access modifier
{
    public int Count;       // SA1401: non-private field
    void Run()
    {
        var s = "";         // SA1122: use string.Empty
        if (Count > 0)
            Run();           // SA1503: omitted braces
        var x = 1;
        var y = 2;          // SA1516: no blank line between members (when applicable)
    }
}
```

Compliant:

```csharp
internal class Service
{
    private int _count;

    public void Run()
    {
        var s = string.Empty;
        if (_count > 0)
        {
            Run();
        }
    }
}
```

---

## Other rules

Any other default StyleCop rule (spacing SA10xx, readability SA11xx, naming SA13xx,
maintainability SA14xx, layout SA15xx) may be reported as a **warning** when clearly
violated, unless it is in the Disabled list above. When in doubt about a rule not
listed here, prefer not to report it.
