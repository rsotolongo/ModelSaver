# ProQualEx.ModelSaver Hidden-Field Persistence Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build ProQualEx.ModelSaver — a NuGet library that automatically emits hidden fields for `[PersistOnPostback]`-marked model properties missing from a rendered ASP.NET MVC form, for both MVC5 and ASP.NET Core.

**Architecture:** A framework-agnostic `ProQualEx.ModelSaver.Core` library holds the attribute, the eligible-path/diff/format/scan algorithm, and the hidden-field HTML renderer. Two thin adapters — `ProQualEx.ModelSaver.Mvc5` (System.Web.Mvc, an output-buffering `IDisposable` form scope) and `ProQualEx.ModelSaver.AspNetCore` (a `<form>` Tag Helper) — call into Core to decide what's missing and splice/append the hidden fields into the form's HTML.

**Tech Stack:** .NET 10 SDK (10.0.111 — the only SDK installed on the dev machine this plan was authored on), C# with `<LangVersion>latest</LangVersion>` everywhere, xUnit 2.9.3, `Microsoft.AspNet.Mvc` 5.2.9 (brings in `System.Web.Mvc`), `Microsoft.NET.Sdk.Razor` + `<FrameworkReference Include="Microsoft.AspNetCore.App" />` for the ASP.NET Core adapter (this combination was spiked and confirmed to compile and unit-test correctly on Linux without a running web server).

**Spec:** [docs/superpowers/specs/2026-09-05-modelsaver-hidden-field-persistence-design.md](../specs/2026-09-05-modelsaver-hidden-field-persistence-design.md)

## Global Constraints

- Target frameworks: `ProQualEx.ModelSaver.Core` → `netstandard2.0`; `ProQualEx.ModelSaver.Core.Tests` → `net10.0`; `ProQualEx.ModelSaver.AspNetCore` and `ProQualEx.ModelSaver.AspNetCore.Tests` → `net10.0` via SDK `Microsoft.NET.Sdk.Razor` + `<FrameworkReference Include="Microsoft.AspNetCore.App" />`; `ProQualEx.ModelSaver.Mvc5` and `ProQualEx.ModelSaver.Mvc5.Tests` → `net472`.
- Every csproj sets `<LangVersion>latest</LangVersion>`, `<Nullable>enable</Nullable>`, and `<ImplicitUsings>enable</ImplicitUsings>`. This was spiked on net472 with the `Microsoft.AspNet.Mvc` package too: it builds clean (nullable annotations just produce ordinary compiler warnings against System.Web.Mvc's unannotated API, never errors).
- No `record`/`init`-only types anywhere in `ProQualEx.ModelSaver.Core` — it targets `netstandard2.0`, which lacks the `IsExternalInit` runtime shim needed for `init` accessors. Use plain classes with constructor-set, get-only properties instead.
- Public API lives directly in the `ProQualEx.ModelSaver` / `ProQualEx.ModelSaver.AspNetCore` / `ProQualEx.ModelSaver.Mvc5` namespaces. Implementation details not meant for consumers live under an `Internal` sub-namespace, are `internal`, and are exposed to their matching test project only via `[assembly: InternalsVisibleTo("...")]` in an `AssemblyInfo.cs` at the project root.
- **Known environment gap:** this machine has no Mono installed, so `dotnet test` cannot execute `net472` binaries here — only `dotnet build` can verify `ProQualEx.ModelSaver.Mvc5`/`ProQualEx.ModelSaver.Mvc5.Tests` compile. Task 9 assumes Mono has been installed by the time it runs (the user was asked to run `sudo apt-get install -y mono-devel` themselves — this is not something an agent should run, as installing system packages is outside what an agent may do unilaterally). If `dotnet test` on that project still reports `Could not find 'mono' host`, fall back to `dotnet build` for verification and say so plainly — do not claim the tests passed if they didn't actually run.
- All commands below assume the working directory is the repo root (`/home/rsotolongo/Projects/ProQualEx.ModelSaver`) unless a step says otherwise. `git commit` messages should NOT include a Co-Authored-By trailer unless the human running this plan asks for one, since it wasn't requested for this repo.

---

### Task 1: Solution scaffold + `ProQualEx.ModelSaver.Core` skeleton

**Files:**
- Create: `.gitignore`
- Create: `ProQualEx.ModelSaver.sln`
- Create: `src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj`
- Create: `src/ProQualEx.ModelSaver.Core/AssemblyInfo.cs`
- Create: `tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
- Create: `tests/ProQualEx.ModelSaver.Core.Tests/ScaffoldSmokeTests.cs`

**Interfaces:**
- Produces: a solution (`ProQualEx.ModelSaver.sln`) that later tasks add projects to; the `ProQualEx.ModelSaver.Core` / `ProQualEx.ModelSaver.Core.Tests` project pair and its `InternalsVisibleTo` wiring, which every later Core task builds on.

- [ ] **Step 1: Create `.gitignore`**

```
bin/
obj/
*.user
.vs/
```

- [ ] **Step 2: Create the solution file**

```bash
dotnet new sln -n ProQualEx.ModelSaver
```

- [ ] **Step 3: Scaffold the `ProQualEx.ModelSaver.Core` project**

```bash
dotnet new classlib -n ProQualEx.ModelSaver.Core -o src/ProQualEx.ModelSaver.Core -f netstandard2.0
rm src/ProQualEx.ModelSaver.Core/Class1.cs
```

Replace the generated `src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

</Project>
```

- [ ] **Step 4: Add the project to the solution**

```bash
dotnet sln ProQualEx.ModelSaver.sln add src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj
```

- [ ] **Step 5: Wire up `InternalsVisibleTo` for the test project**

Create `src/ProQualEx.ModelSaver.Core/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ProQualEx.ModelSaver.Core.Tests")]
```

- [ ] **Step 6: Scaffold the `ProQualEx.ModelSaver.Core.Tests` project**

```bash
dotnet new xunit -n ProQualEx.ModelSaver.Core.Tests -o tests/ProQualEx.ModelSaver.Core.Tests -f net10.0
rm tests/ProQualEx.ModelSaver.Core.Tests/UnitTest1.cs
```

Replace the generated `tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj" />
  </ItemGroup>

</Project>
```

- [ ] **Step 7: Add the test project to the solution**

```bash
dotnet sln ProQualEx.ModelSaver.sln add tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj
```

- [ ] **Step 8: Write a scaffold smoke test**

Create `tests/ProQualEx.ModelSaver.Core.Tests/ScaffoldSmokeTests.cs`:

```csharp
namespace ProQualEx.ModelSaver.Core.Tests;

public class ScaffoldSmokeTests
{
    [Fact]
    public void TestHarness_IsWiredUp()
    {
        Assert.Equal(4, 2 + 2);
    }
}
```

This has nothing to do with the real feature — it only proves the solution/build/test pipeline works before any real logic exists.

- [ ] **Step 9: Run the tests, verify PASS**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: `Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1`

- [ ] **Step 10: Commit**

```bash
git add .gitignore ProQualEx.ModelSaver.sln src/ProQualEx.ModelSaver.Core tests/ProQualEx.ModelSaver.Core.Tests
git commit -m "Scaffold ProQualEx.ModelSaver.Core project and test harness"
```

---

### Task 2: `PersistOnPostbackAttribute` + attribute resolution

**Files:**
- Create: `src/ProQualEx.ModelSaver.Core/PersistOnPostbackAttribute.cs`
- Create: `src/ProQualEx.ModelSaver.Core/Internal/PersistenceResolver.cs`
- Test: `tests/ProQualEx.ModelSaver.Core.Tests/PersistenceResolverTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks (this is the foundation).
- Produces: `ProQualEx.ModelSaver.PersistOnPostbackAttribute` (public, usable on `Class` or `Property`, constructor `PersistOnPostbackAttribute(bool persist = true)`, property `bool Persist`) and `ProQualEx.ModelSaver.Internal.PersistenceResolver.ShouldPersist(PropertyInfo property) : bool`, which Task 3's path collector depends on directly.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProQualEx.ModelSaver.Core.Tests/PersistenceResolverTests.cs`:

```csharp
using ProQualEx.ModelSaver;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class NoAttributesModel
{
    public string Name { get; set; } = "";
}

[PersistOnPostback]
public class ClassDefaultModel
{
    public string IncludedByDefault { get; set; } = "";

    [PersistOnPostback(false)]
    public string ExcludedByOverride { get; set; } = "";
}

public class PropertyOnlyModel
{
    [PersistOnPostback]
    public string IncludedExplicitly { get; set; } = "";

    public string NotMarked { get; set; } = "";
}

[PersistOnPostback(false)]
public class ClassDefaultFalseModel
{
    public string NotPersistedByDefault { get; set; } = "";

    [PersistOnPostback]
    public string PersistedByOverride { get; set; } = "";
}

public class PersistenceResolverTests
{
    [Fact]
    public void ShouldPersist_NoAttributesAnywhere_ReturnsFalse()
    {
        var property = typeof(NoAttributesModel).GetProperty(nameof(NoAttributesModel.Name))!;
        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_ClassLevelDefaultTrue_AppliesToUnmarkedProperty()
    {
        var property = typeof(ClassDefaultModel).GetProperty(nameof(ClassDefaultModel.IncludedByDefault))!;
        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_PropertyOverridesClassDefault_ToFalse()
    {
        var property = typeof(ClassDefaultModel).GetProperty(nameof(ClassDefaultModel.ExcludedByOverride))!;
        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_PropertyOnlyAttribute_ReturnsTrueForMarkedProperty()
    {
        var property = typeof(PropertyOnlyModel).GetProperty(nameof(PropertyOnlyModel.IncludedExplicitly))!;
        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_PropertyOnlyAttribute_ReturnsFalseForUnmarkedProperty()
    {
        var property = typeof(PropertyOnlyModel).GetProperty(nameof(PropertyOnlyModel.NotMarked))!;
        Assert.False(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_ClassLevelDefaultFalse_PropertyOverridesToTrue()
    {
        var property = typeof(ClassDefaultFalseModel).GetProperty(nameof(ClassDefaultFalseModel.PersistedByOverride))!;
        Assert.True(PersistenceResolver.ShouldPersist(property));
    }

    [Fact]
    public void ShouldPersist_ClassLevelDefaultFalse_UnmarkedPropertyReturnsFalse()
    {
        var property = typeof(ClassDefaultFalseModel).GetProperty(nameof(ClassDefaultFalseModel.NotPersistedByDefault))!;
        Assert.False(PersistenceResolver.ShouldPersist(property));
    }
}
```

- [ ] **Step 2: Run tests, verify they fail to compile**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: FAIL — `PersistOnPostbackAttribute` and `PersistenceResolver` do not exist yet.

- [ ] **Step 3: Implement `PersistOnPostbackAttribute`**

Create `src/ProQualEx.ModelSaver.Core/PersistOnPostbackAttribute.cs`:

```csharp
using System;

namespace ProQualEx.ModelSaver;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public sealed class PersistOnPostbackAttribute : Attribute
{
    public bool Persist { get; }

    public PersistOnPostbackAttribute(bool persist = true)
    {
        Persist = persist;
    }
}
```

- [ ] **Step 4: Implement `PersistenceResolver`**

Create `src/ProQualEx.ModelSaver.Core/Internal/PersistenceResolver.cs`:

```csharp
using System;
using System.Reflection;

namespace ProQualEx.ModelSaver.Internal;

internal static class PersistenceResolver
{
    public static bool ShouldPersist(PropertyInfo property)
    {
        if (property is null)
        {
            throw new ArgumentNullException(nameof(property));
        }

        var propertyAttribute = property.GetCustomAttribute<PersistOnPostbackAttribute>(inherit: true);
        if (propertyAttribute is not null)
        {
            return propertyAttribute.Persist;
        }

        var classAttribute = property.DeclaringType?.GetCustomAttribute<PersistOnPostbackAttribute>(inherit: true);
        return classAttribute?.Persist ?? false;
    }
}
```

- [ ] **Step 5: Run tests, verify PASS**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: all 7 tests pass.

- [ ] **Step 6: Commit**

```bash
git add src/ProQualEx.ModelSaver.Core tests/ProQualEx.ModelSaver.Core.Tests
git commit -m "Add PersistOnPostbackAttribute and its resolution rules"
```

---

### Task 3: Eligible-path collection (flat + nested objects)

**Files:**
- Create: `src/ProQualEx.ModelSaver.Core/Internal/ScalarTypes.cs`
- Create: `src/ProQualEx.ModelSaver.Core/Internal/EligiblePath.cs`
- Create: `src/ProQualEx.ModelSaver.Core/Internal/EligiblePathCollector.cs`
- Create: `tests/ProQualEx.ModelSaver.Core.Tests/TestModels.cs`
- Test: `tests/ProQualEx.ModelSaver.Core.Tests/EligiblePathCollectorTests.cs`

**Interfaces:**
- Consumes: `ProQualEx.ModelSaver.Internal.PersistenceResolver.ShouldPersist(PropertyInfo)` from Task 2.
- Produces: `ProQualEx.ModelSaver.Internal.EligiblePath` (properties `string Name`, `object? Value`, `Type ValueType`) and `ProQualEx.ModelSaver.Internal.EligiblePathCollector.Collect(object model) : IReadOnlyList<EligiblePath>`, which Task 6's `HiddenFieldDiffer` depends on directly. Also produces `tests/ProQualEx.ModelSaver.Core.Tests/TestModels.cs`, a shared fixture file that Task 6's tests reuse (`FlatModel`, `AddressModel`, `NoAttributeAddressModel`, `ParentWithNestedModel`, `ParentWithNestedNoAttributeModel`).

- [ ] **Step 1: Write the shared test model fixtures**

Create `tests/ProQualEx.ModelSaver.Core.Tests/TestModels.cs`:

```csharp
using ProQualEx.ModelSaver;

namespace ProQualEx.ModelSaver.Core.Tests;

[PersistOnPostback]
public class FlatModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";

    [PersistOnPostback(false)]
    public string Ignored { get; set; } = "";
}

[PersistOnPostback]
public class AddressModel
{
    public string City { get; set; } = "";
}

public class NoAttributeAddressModel
{
    public string City { get; set; } = "";
}

public class ParentWithNestedModel
{
    [PersistOnPostback]
    public AddressModel? Address { get; set; }
}

public class ParentWithNestedNoAttributeModel
{
    [PersistOnPostback]
    public NoAttributeAddressModel? Address { get; set; }
}
```

- [ ] **Step 2: Write the failing tests**

Create `tests/ProQualEx.ModelSaver.Core.Tests/EligiblePathCollectorTests.cs`:

```csharp
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class EligiblePathCollectorTests
{
    [Fact]
    public void Collect_FlatModel_ReturnsOnlyEligibleScalarProperties()
    {
        var model = new FlatModel { Id = 42, Name = "Ada", Ignored = "skip-me" };

        var paths = EligiblePathCollector.Collect(model);

        Assert.Equal(2, paths.Count);
        Assert.Contains(paths, p => p.Name == "Id" && Equals(p.Value, 42));
        Assert.Contains(paths, p => p.Name == "Name" && Equals(p.Value, "Ada"));
        Assert.DoesNotContain(paths, p => p.Name == "Ignored");
    }

    [Fact]
    public void Collect_NestedComplexProperty_BuildsDottedPath()
    {
        var model = new ParentWithNestedModel { Address = new AddressModel { City = "Miami" } };

        var paths = EligiblePathCollector.Collect(model);

        Assert.Single(paths);
        Assert.Equal("Address.City", paths[0].Name);
        Assert.Equal("Miami", paths[0].Value);
    }

    [Fact]
    public void Collect_NullNestedComplexProperty_IsSkippedEntirely()
    {
        var model = new ParentWithNestedModel { Address = null };

        var paths = EligiblePathCollector.Collect(model);

        Assert.Empty(paths);
    }

    [Fact]
    public void Collect_NestedType_UsesItsOwnAttributesNotParentDefault()
    {
        var model = new ParentWithNestedNoAttributeModel
        {
            Address = new NoAttributeAddressModel { City = "Miami" }
        };

        var paths = EligiblePathCollector.Collect(model);

        Assert.Empty(paths);
    }
}
```

- [ ] **Step 3: Run tests, verify they fail to compile**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: FAIL — `EligiblePathCollector` does not exist yet.

- [ ] **Step 4: Implement `ScalarTypes`**

Create `src/ProQualEx.ModelSaver.Core/Internal/ScalarTypes.cs`:

```csharp
using System;

namespace ProQualEx.ModelSaver.Internal;

internal static class ScalarTypes
{
    public static bool IsScalar(Type type)
    {
        if (type is null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(TimeSpan)
            || underlying == typeof(Guid);
    }
}
```

- [ ] **Step 5: Implement `EligiblePath`**

Create `src/ProQualEx.ModelSaver.Core/Internal/EligiblePath.cs`:

```csharp
using System;

namespace ProQualEx.ModelSaver.Internal;

internal sealed class EligiblePath
{
    public string Name { get; }
    public object? Value { get; }
    public Type ValueType { get; }

    public EligiblePath(string name, object? value, Type valueType)
    {
        Name = name;
        Value = value;
        ValueType = valueType;
    }
}
```

- [ ] **Step 6: Implement `EligiblePathCollector`**

Create `src/ProQualEx.ModelSaver.Core/Internal/EligiblePathCollector.cs`:

```csharp
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ProQualEx.ModelSaver.Internal;

internal static class EligiblePathCollector
{
    public static IReadOnlyList<EligiblePath> Collect(object model)
    {
        var results = new List<EligiblePath>();
        CollectInto(model, prefix: "", results);
        return results;
    }

    private static void CollectInto(object model, string prefix, List<EligiblePath> results)
    {
        var properties = model.GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);

        foreach (var property in properties)
        {
            if (!PersistenceResolver.ShouldPersist(property))
            {
                continue;
            }

            var name = prefix.Length == 0 ? property.Name : $"{prefix}.{property.Name}";
            var value = property.GetValue(model);

            if (ScalarTypes.IsScalar(property.PropertyType))
            {
                results.Add(new EligiblePath(name, value, property.PropertyType));
            }
            else if (value is not null)
            {
                CollectInto(value, name, results);
            }
        }
    }
}
```

- [ ] **Step 7: Run tests, verify PASS**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: all tests pass (11 total so far).

- [ ] **Step 8: Commit**

```bash
git add src/ProQualEx.ModelSaver.Core tests/ProQualEx.ModelSaver.Core.Tests
git commit -m "Add eligible-path collection for flat and nested-object properties"
```

---

### Task 4: Value formatting

**Files:**
- Create: `src/ProQualEx.ModelSaver.Core/Internal/PersistValueFormatter.cs`
- Test: `tests/ProQualEx.ModelSaver.Core.Tests/PersistValueFormatterTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `ProQualEx.ModelSaver.Internal.PersistValueFormatter.Format(object? value) : string`, which Task 6's `HiddenFieldDiffer` depends on directly.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProQualEx.ModelSaver.Core.Tests/PersistValueFormatterTests.cs`:

```csharp
using System;
using System.Globalization;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class PersistValueFormatterTests
{
    [Fact]
    public void Format_Null_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, PersistValueFormatter.Format(null));
    }

    [Fact]
    public void Format_String_ReturnsSameString()
    {
        Assert.Equal("Ada", PersistValueFormatter.Format("Ada"));
    }

    [Fact]
    public void Format_Int_ReturnsDigits()
    {
        Assert.Equal("42", PersistValueFormatter.Format(42));
    }

    [Fact]
    public void Format_Bool_ReturnsTrueOrFalseText()
    {
        Assert.Equal("True", PersistValueFormatter.Format(true));
    }

    [Fact]
    public void Format_Guid_ReturnsGuidString()
    {
        var guid = Guid.Parse("00000000-0000-0000-0000-000000000001");
        Assert.Equal(guid.ToString(), PersistValueFormatter.Format(guid));
    }

    [Fact]
    public void Format_DateTime_MatchesCurrentCultureConversion()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("en-US");
            var date = new DateTime(2026, 3, 5);

            var expected = Convert.ToString(date, CultureInfo.CurrentCulture);
            Assert.Equal(expected, PersistValueFormatter.Format(date));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
```

- [ ] **Step 2: Run tests, verify they fail to compile**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: FAIL — `PersistValueFormatter` does not exist yet.

- [ ] **Step 3: Implement `PersistValueFormatter`**

Create `src/ProQualEx.ModelSaver.Core/Internal/PersistValueFormatter.cs`:

```csharp
using System;
using System.Globalization;

namespace ProQualEx.ModelSaver.Internal;

internal static class PersistValueFormatter
{
    public static string Format(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return Convert.ToString(value, CultureInfo.CurrentCulture) ?? string.Empty;
    }
}
```

- [ ] **Step 4: Run tests, verify PASS**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: all tests pass (17 total so far).

- [ ] **Step 5: Commit**

```bash
git add src/ProQualEx.ModelSaver.Core tests/ProQualEx.ModelSaver.Core.Tests
git commit -m "Add value formatting matching HiddenFor's CurrentCulture convention"
```

---

### Task 5: Rendered-name scanning

**Files:**
- Create: `src/ProQualEx.ModelSaver.Core/Internal/RenderedNameScanner.cs`
- Test: `tests/ProQualEx.ModelSaver.Core.Tests/RenderedNameScannerTests.cs`

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `ProQualEx.ModelSaver.Internal.RenderedNameScanner.ExtractNames(string html) : HashSet<string>`, which Task 6's `HiddenFieldDiffer` depends on directly.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProQualEx.ModelSaver.Core.Tests/RenderedNameScannerTests.cs`:

```csharp
using System.Collections.Generic;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver.Core.Tests;

public class RenderedNameScannerTests
{
    [Fact]
    public void ExtractNames_DoubleQuotedInput_IsExtracted()
    {
        var html = "<input type=\"text\" name=\"Name\" value=\"Ada\" />";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Contains("Name", names);
    }

    [Fact]
    public void ExtractNames_SingleQuotedInput_IsExtracted()
    {
        var html = "<input type='text' name='Name' value='Ada' />";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Contains("Name", names);
    }

    [Fact]
    public void ExtractNames_SelectAndTextarea_AreExtracted()
    {
        var html = "<select name=\"Country\"></select><textarea name=\"Bio\"></textarea>";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Contains("Country", names);
        Assert.Contains("Bio", names);
    }

    [Theory]
    [InlineData("submit")]
    [InlineData("button")]
    [InlineData("reset")]
    [InlineData("image")]
    public void ExtractNames_ExcludesNonDataInputTypes(string inputType)
    {
        var html = $"<input type=\"{inputType}\" name=\"Save\" value=\"Save\" />";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.DoesNotContain("Save", names);
    }

    [Fact]
    public void ExtractNames_NonSelfClosingInputTag_IsExtracted()
    {
        var html = "<input type=\"text\" name=\"Name\" value=\"Ada\">";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Contains("Name", names);
    }

    [Fact]
    public void ExtractNames_MultipleElements_ReturnsAllNames()
    {
        var html = "<input name=\"A\" /><input name=\"B\" />";
        var names = RenderedNameScanner.ExtractNames(html);
        Assert.Equal(new HashSet<string> { "A", "B" }, names);
    }
}
```

- [ ] **Step 2: Run tests, verify they fail to compile**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: FAIL — `RenderedNameScanner` does not exist yet.

- [ ] **Step 3: Implement `RenderedNameScanner`**

Create `src/ProQualEx.ModelSaver.Core/Internal/RenderedNameScanner.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ProQualEx.ModelSaver.Internal;

internal static class RenderedNameScanner
{
    private static readonly Regex NamedElementPattern = new Regex(
        @"<(input|select|textarea)\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex AttributePattern = new Regex(
        @"(\w[\w-]*)\s*=\s*(""([^""]*)""|'([^']*)')",
        RegexOptions.Compiled);

    private static readonly HashSet<string> ExcludedInputTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "submit", "button", "reset", "image"
    };

    public static HashSet<string> ExtractNames(string html)
    {
        if (html is null)
        {
            throw new ArgumentNullException(nameof(html));
        }

        var names = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match element in NamedElementPattern.Matches(html))
        {
            var attributes = ParseAttributes(element.Value);

            if (attributes.TryGetValue("type", out var type) && ExcludedInputTypes.Contains(type))
            {
                continue;
            }

            if (attributes.TryGetValue("name", out var name) && name.Length > 0)
            {
                names.Add(name);
            }
        }

        return names;
    }

    private static Dictionary<string, string> ParseAttributes(string elementHtml)
    {
        var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match match in AttributePattern.Matches(elementHtml))
        {
            var attributeName = match.Groups[1].Value;
            var value = match.Groups[3].Success ? match.Groups[3].Value : match.Groups[4].Value;
            attributes[attributeName] = value;
        }

        return attributes;
    }
}
```

- [ ] **Step 4: Run tests, verify PASS**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: all tests pass (23 total so far).

- [ ] **Step 5: Commit**

```bash
git add src/ProQualEx.ModelSaver.Core tests/ProQualEx.ModelSaver.Core.Tests
git commit -m "Add rendered-name scanning to detect fields already on the form"
```

---

### Task 6: `HiddenFieldDiffer` (public diff API)

**Files:**
- Create: `src/ProQualEx.ModelSaver.Core/PersistField.cs`
- Create: `src/ProQualEx.ModelSaver.Core/HiddenFieldDiffer.cs`
- Test: `tests/ProQualEx.ModelSaver.Core.Tests/HiddenFieldDifferTests.cs`

**Interfaces:**
- Consumes: `EligiblePathCollector.Collect` (Task 3), `PersistValueFormatter.Format` (Task 4), `RenderedNameScanner.ExtractNames` (Task 5).
- Produces: `ProQualEx.ModelSaver.PersistField` (public, properties `string Name`, `string Value`, constructor `PersistField(string name, string value)`) and `ProQualEx.ModelSaver.HiddenFieldDiffer.GetMissingFields(object model, string renderedFormHtml) : IReadOnlyList<PersistField>` — the single public entry point both framework adapters (Tasks 8 and 9) call.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProQualEx.ModelSaver.Core.Tests/HiddenFieldDifferTests.cs`:

```csharp
using System;
using ProQualEx.ModelSaver;

namespace ProQualEx.ModelSaver.Core.Tests;

public class HiddenFieldDifferTests
{
    [Fact]
    public void GetMissingFields_PropertyAlreadyRendered_IsExcluded()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };
        var renderedHtml = "<input type=\"text\" name=\"Name\" value=\"Ada\" />";

        var missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Single(missing);
        Assert.Equal("Id", missing[0].Name);
        Assert.Equal("42", missing[0].Value);
    }

    [Fact]
    public void GetMissingFields_NothingRendered_ReturnsAllEligibleFields()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };

        var missing = HiddenFieldDiffer.GetMissingFields(model, renderedFormHtml: "");

        Assert.Equal(2, missing.Count);
    }

    [Fact]
    public void GetMissingFields_EverythingRendered_ReturnsEmpty()
    {
        var model = new FlatModel { Id = 42, Name = "Ada" };
        var renderedHtml = "<input name=\"Id\" /><input name=\"Name\" />";

        var missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Empty(missing);
    }

    [Fact]
    public void GetMissingFields_NestedProperty_UsesDottedNameForDiff()
    {
        var model = new ParentWithNestedModel { Address = new AddressModel { City = "Miami" } };
        var renderedHtml = "<input name=\"Address.City\" />";

        var missing = HiddenFieldDiffer.GetMissingFields(model, renderedHtml);

        Assert.Empty(missing);
    }

    [Fact]
    public void GetMissingFields_NullModel_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => HiddenFieldDiffer.GetMissingFields(null!, ""));
    }
}
```

- [ ] **Step 2: Run tests, verify they fail to compile**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: FAIL — `PersistField` and `HiddenFieldDiffer` do not exist yet.

- [ ] **Step 3: Implement `PersistField`**

Create `src/ProQualEx.ModelSaver.Core/PersistField.cs`:

```csharp
using System;

namespace ProQualEx.ModelSaver;

public sealed class PersistField
{
    public string Name { get; }
    public string Value { get; }

    public PersistField(string name, string value)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }
}
```

- [ ] **Step 4: Implement `HiddenFieldDiffer`**

Create `src/ProQualEx.ModelSaver.Core/HiddenFieldDiffer.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using ProQualEx.ModelSaver.Internal;

namespace ProQualEx.ModelSaver;

public static class HiddenFieldDiffer
{
    public static IReadOnlyList<PersistField> GetMissingFields(object model, string renderedFormHtml)
    {
        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        if (renderedFormHtml is null)
        {
            throw new ArgumentNullException(nameof(renderedFormHtml));
        }

        var eligiblePaths = EligiblePathCollector.Collect(model);
        var renderedNames = RenderedNameScanner.ExtractNames(renderedFormHtml);

        return eligiblePaths
            .Where(path => !renderedNames.Contains(path.Name))
            .Select(path => new PersistField(path.Name, PersistValueFormatter.Format(path.Value)))
            .ToList();
    }
}
```

- [ ] **Step 5: Run tests, verify PASS**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: all tests pass (28 total so far).

- [ ] **Step 6: Commit**

```bash
git add src/ProQualEx.ModelSaver.Core tests/ProQualEx.ModelSaver.Core.Tests
git commit -m "Add HiddenFieldDiffer, the public entry point tying the algorithm together"
```

---

### Task 7: `HiddenFieldRenderer` + Core packaging metadata (completes `ProQualEx.ModelSaver.Core`)

**Files:**
- Create: `src/ProQualEx.ModelSaver.Core/HiddenFieldRenderer.cs`
- Modify: `src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj`
- Test: `tests/ProQualEx.ModelSaver.Core.Tests/HiddenFieldRendererTests.cs`

**Interfaces:**
- Consumes: `ProQualEx.ModelSaver.PersistField` (Task 6).
- Produces: `ProQualEx.ModelSaver.HiddenFieldRenderer.Render(IEnumerable<PersistField> fields) : string`, which both adapters (Tasks 8 and 9) call to turn `HiddenFieldDiffer`'s output into HTML.

- [ ] **Step 1: Write the failing tests**

Create `tests/ProQualEx.ModelSaver.Core.Tests/HiddenFieldRendererTests.cs`:

```csharp
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ProQualEx.ModelSaver;

namespace ProQualEx.ModelSaver.Core.Tests;

public class HiddenFieldRendererTests
{
    [Fact]
    public void Render_SingleField_ProducesHiddenInputWithNameIdAndValue()
    {
        var fields = new List<PersistField> { new PersistField("Id", "42") };

        var html = HiddenFieldRenderer.Render(fields);

        Assert.Equal("<input type=\"hidden\" name=\"Id\" id=\"Id\" value=\"42\" />", html);
    }

    [Fact]
    public void Render_DottedName_ReplacesDotsWithUnderscoresInId()
    {
        var fields = new List<PersistField> { new PersistField("Address.City", "Miami") };

        var html = HiddenFieldRenderer.Render(fields);

        Assert.Contains("name=\"Address.City\"", html);
        Assert.Contains("id=\"Address_City\"", html);
    }

    [Fact]
    public void Render_ValueWithHtmlSpecialCharacters_IsEncoded()
    {
        var fields = new List<PersistField> { new PersistField("Bio", "<script>alert(1)</script>") };

        var html = HiddenFieldRenderer.Render(fields);

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Render_MultipleFields_ProducesOneInputPerField()
    {
        var fields = new List<PersistField>
        {
            new PersistField("Id", "42"),
            new PersistField("Name", "Ada")
        };

        var html = HiddenFieldRenderer.Render(fields);

        Assert.Equal(2, Regex.Matches(html, "<input").Count);
    }

    [Fact]
    public void Render_EmptyList_ProducesEmptyString()
    {
        Assert.Equal(string.Empty, HiddenFieldRenderer.Render(new List<PersistField>()));
    }
}
```

- [ ] **Step 2: Run tests, verify they fail to compile**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: FAIL — `HiddenFieldRenderer` does not exist yet.

- [ ] **Step 3: Implement `HiddenFieldRenderer`**

Create `src/ProQualEx.ModelSaver.Core/HiddenFieldRenderer.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace ProQualEx.ModelSaver;

public static class HiddenFieldRenderer
{
    public static string Render(IEnumerable<PersistField> fields)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        var builder = new StringBuilder();

        foreach (var field in fields)
        {
            var id = field.Name.Replace('.', '_');

            builder.Append("<input type=\"hidden\" name=\"")
                .Append(WebUtility.HtmlEncode(field.Name))
                .Append("\" id=\"")
                .Append(WebUtility.HtmlEncode(id))
                .Append("\" value=\"")
                .Append(WebUtility.HtmlEncode(field.Value))
                .Append("\" />");
        }

        return builder.ToString();
    }
}
```

- [ ] **Step 4: Run tests, verify PASS**

Run: `dotnet test tests/ProQualEx.ModelSaver.Core.Tests/ProQualEx.ModelSaver.Core.Tests.csproj`
Expected: all tests pass (33 total so far). `ProQualEx.ModelSaver.Core`'s v1 public API is now complete: `PersistOnPostbackAttribute`, `PersistField`, `HiddenFieldDiffer`, `HiddenFieldRenderer`.

- [ ] **Step 5: Add NuGet packaging metadata**

In `src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj`, add inside the existing `<PropertyGroup>`:

```xml
    <PackageId>ProQualEx.ModelSaver.Core</PackageId>
    <Version>0.1.0</Version>
    <Description>Framework-agnostic core for ProQualEx.ModelSaver: attribute-driven automatic hidden-field persistence for ASP.NET MVC forms.</Description>
```

- [ ] **Step 6: Verify the package still builds**

Run: `dotnet build src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj`
Expected: `Build succeeded.`

- [ ] **Step 7: Commit**

```bash
git add src/ProQualEx.ModelSaver.Core tests/ProQualEx.ModelSaver.Core.Tests
git commit -m "Add HiddenFieldRenderer and packaging metadata, completing ProQualEx.ModelSaver.Core v1"
```

---

### Task 8: `ProQualEx.ModelSaver.AspNetCore` adapter (Tag Helper)

**Files:**
- Create: `src/ProQualEx.ModelSaver.AspNetCore/ProQualEx.ModelSaver.AspNetCore.csproj`
- Create: `src/ProQualEx.ModelSaver.AspNetCore/PersistOnPostbackTagHelper.cs`
- Create: `tests/ProQualEx.ModelSaver.AspNetCore.Tests/ProQualEx.ModelSaver.AspNetCore.Tests.csproj`
- Test: `tests/ProQualEx.ModelSaver.AspNetCore.Tests/PersistOnPostbackTagHelperTests.cs`
- Modify: `ProQualEx.ModelSaver.sln`

**Interfaces:**
- Consumes: `ProQualEx.ModelSaver.HiddenFieldDiffer.GetMissingFields` and `ProQualEx.ModelSaver.HiddenFieldRenderer.Render` (Task 6, Task 7).
- Produces: `ProQualEx.ModelSaver.AspNetCore.PersistOnPostbackTagHelper`, a public Tag Helper — no later task depends on it, it's a leaf adapter.

- [ ] **Step 1: Scaffold `ProQualEx.ModelSaver.AspNetCore`**

```bash
dotnet new classlib -n ProQualEx.ModelSaver.AspNetCore -o src/ProQualEx.ModelSaver.AspNetCore -f net10.0
rm src/ProQualEx.ModelSaver.AspNetCore/Class1.cs
```

Replace `src/ProQualEx.ModelSaver.AspNetCore/ProQualEx.ModelSaver.AspNetCore.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PackageId>ProQualEx.ModelSaver.AspNetCore</PackageId>
    <Version>0.1.0</Version>
    <Description>ASP.NET Core Tag Helper adapter for ProQualEx.ModelSaver: automatic hidden-field persistence for &lt;form&gt; elements.</Description>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj" />
  </ItemGroup>

</Project>
```

```bash
dotnet sln ProQualEx.ModelSaver.sln add src/ProQualEx.ModelSaver.AspNetCore/ProQualEx.ModelSaver.AspNetCore.csproj
```

- [ ] **Step 2: Scaffold `ProQualEx.ModelSaver.AspNetCore.Tests`**

```bash
dotnet new xunit -n ProQualEx.ModelSaver.AspNetCore.Tests -o tests/ProQualEx.ModelSaver.AspNetCore.Tests -f net10.0
rm tests/ProQualEx.ModelSaver.AspNetCore.Tests/UnitTest1.cs
```

Replace `tests/ProQualEx.ModelSaver.AspNetCore.Tests/ProQualEx.ModelSaver.AspNetCore.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj" />
    <ProjectReference Include="../../src/ProQualEx.ModelSaver.AspNetCore/ProQualEx.ModelSaver.AspNetCore.csproj" />
  </ItemGroup>

</Project>
```

```bash
dotnet sln ProQualEx.ModelSaver.sln add tests/ProQualEx.ModelSaver.AspNetCore.Tests/ProQualEx.ModelSaver.AspNetCore.Tests.csproj
```

- [ ] **Step 3: Write the failing tests**

Create `tests/ProQualEx.ModelSaver.AspNetCore.Tests/PersistOnPostbackTagHelperTests.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ProQualEx.ModelSaver;
using ProQualEx.ModelSaver.AspNetCore;

namespace ProQualEx.ModelSaver.AspNetCore.Tests;

public class PersistOnPostbackTagHelperTests
{
    [PersistOnPostback]
    private class OrderModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
    }

    [Fact]
    public async Task ProcessAsync_AppendsHiddenFieldsForMissingProperties()
    {
        var helper = new PersistOnPostbackTagHelper
        {
            Model = new OrderModel { Id = 7, CustomerName = "Ada" }
        };
        var output = CreateOutput("<input name=\"CustomerName\" value=\"Ada\" />");

        await helper.ProcessAsync(CreateContext(), output);

        var rendered = output.PostContent.GetContent();
        Assert.Contains("name=\"Id\"", rendered);
        Assert.DoesNotContain("name=\"CustomerName\"", rendered);
    }

    [Fact]
    public async Task ProcessAsync_EverythingAlreadyRendered_AppendsNothing()
    {
        var helper = new PersistOnPostbackTagHelper
        {
            Model = new OrderModel { Id = 7, CustomerName = "Ada" }
        };
        var output = CreateOutput("<input name=\"Id\" /><input name=\"CustomerName\" />");

        await helper.ProcessAsync(CreateContext(), output);

        Assert.Equal(string.Empty, output.PostContent.GetContent());
    }

    [Fact]
    public async Task ProcessAsync_NullModel_DoesNothing()
    {
        var helper = new PersistOnPostbackTagHelper { Model = null };
        var output = CreateOutput("<input name=\"Id\" />");

        await helper.ProcessAsync(CreateContext(), output);

        Assert.Equal(string.Empty, output.PostContent.GetContent());
    }

    private static TagHelperContext CreateContext() =>
        new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString("N"));

    private static TagHelperOutput CreateOutput(string childContent) =>
        new TagHelperOutput(
            "form",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
                Task.FromResult<TagHelperContent>(new DefaultTagHelperContent().SetHtmlContent(childContent)));
}
```

- [ ] **Step 4: Run tests, verify they fail to compile**

Run: `dotnet test tests/ProQualEx.ModelSaver.AspNetCore.Tests/ProQualEx.ModelSaver.AspNetCore.Tests.csproj`
Expected: FAIL — `PersistOnPostbackTagHelper` does not exist yet.

- [ ] **Step 5: Implement `PersistOnPostbackTagHelper`**

Create `src/ProQualEx.ModelSaver.AspNetCore/PersistOnPostbackTagHelper.cs`:

```csharp
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.TagHelpers;
using ProQualEx.ModelSaver;

namespace ProQualEx.ModelSaver.AspNetCore;

[HtmlTargetElement("form", Attributes = "asp-persist-model")]
public class PersistOnPostbackTagHelper : TagHelper
{
    [HtmlAttributeName("asp-persist-model")]
    public object? Model { get; set; }

    public override int Order => int.MaxValue;

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        if (Model is null)
        {
            return;
        }

        var content = await output.GetChildContentAsync();
        var renderedHtml = content.GetContent();
        var missingFields = HiddenFieldDiffer.GetMissingFields(Model, renderedHtml);

        if (missingFields.Count > 0)
        {
            output.PostContent.AppendHtml(HiddenFieldRenderer.Render(missingFields));
        }
    }
}
```

Usage in a real view would look like: `<form asp-action="Save" asp-persist-model="Model"> ... </form>`.

- [ ] **Step 6: Run tests, verify PASS**

Run: `dotnet test tests/ProQualEx.ModelSaver.AspNetCore.Tests/ProQualEx.ModelSaver.AspNetCore.Tests.csproj`
Expected: all 3 tests pass.

- [ ] **Step 7: Commit**

```bash
git add ProQualEx.ModelSaver.sln src/ProQualEx.ModelSaver.AspNetCore tests/ProQualEx.ModelSaver.AspNetCore.Tests
git commit -m "Add ProQualEx.ModelSaver.AspNetCore Tag Helper adapter"
```

---

### Task 9: `ProQualEx.ModelSaver.Mvc5` adapter (`BeginPersistedForm`)

**Files:**
- Create: `src/ProQualEx.ModelSaver.Mvc5/ProQualEx.ModelSaver.Mvc5.csproj`
- Create: `src/ProQualEx.ModelSaver.Mvc5/AssemblyInfo.cs`
- Create: `src/ProQualEx.ModelSaver.Mvc5/Internal/FormHtmlSplicer.cs`
- Create: `src/ProQualEx.ModelSaver.Mvc5/Internal/PersistedFormScope.cs`
- Create: `src/ProQualEx.ModelSaver.Mvc5/PersistedFormExtensions.cs`
- Create: `tests/ProQualEx.ModelSaver.Mvc5.Tests/ProQualEx.ModelSaver.Mvc5.Tests.csproj`
- Test: `tests/ProQualEx.ModelSaver.Mvc5.Tests/FormHtmlSplicerTests.cs`
- Modify: `ProQualEx.ModelSaver.sln`

**Interfaces:**
- Consumes: `ProQualEx.ModelSaver.HiddenFieldDiffer.GetMissingFields` and `ProQualEx.ModelSaver.HiddenFieldRenderer.Render` (Task 6, Task 7).
- Produces: `ProQualEx.ModelSaver.Mvc5.PersistedFormExtensions.BeginPersistedForm(this HtmlHelper, object model, string actionName, string controllerName, FormMethod method = FormMethod.Post, object? htmlAttributes = null) : IDisposable` — no later task depends on it, it's a leaf adapter.

- [ ] **Step 1: Scaffold `ProQualEx.ModelSaver.Mvc5`**

```bash
dotnet new classlib -n ProQualEx.ModelSaver.Mvc5 -o src/ProQualEx.ModelSaver.Mvc5 -f netstandard2.0
rm src/ProQualEx.ModelSaver.Mvc5/Class1.cs
```

Replace `src/ProQualEx.ModelSaver.Mvc5/ProQualEx.ModelSaver.Mvc5.csproj` with (note: `TargetFramework` is `net472`, not what `dotnet new` generated — the `classlib` template doesn't offer `net472` as a `-f` value, so it's created as `netstandard2.0` and retargeted here):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PackageId>ProQualEx.ModelSaver.Mvc5</PackageId>
    <Version>0.1.0</Version>
    <Description>ASP.NET MVC 5 adapter for ProQualEx.ModelSaver: automatic hidden-field persistence for Html.BeginForm.</Description>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNet.Mvc" Version="5.2.9" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj" />
  </ItemGroup>

</Project>
```

```bash
dotnet sln ProQualEx.ModelSaver.sln add src/ProQualEx.ModelSaver.Mvc5/ProQualEx.ModelSaver.Mvc5.csproj
```

- [ ] **Step 2: Wire up `InternalsVisibleTo`**

Create `src/ProQualEx.ModelSaver.Mvc5/AssemblyInfo.cs`:

```csharp
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("ProQualEx.ModelSaver.Mvc5.Tests")]
```

- [ ] **Step 3: Scaffold `ProQualEx.ModelSaver.Mvc5.Tests`**

```bash
dotnet new xunit -n ProQualEx.ModelSaver.Mvc5.Tests -o tests/ProQualEx.ModelSaver.Mvc5.Tests -f netstandard2.0
rm tests/ProQualEx.ModelSaver.Mvc5.Tests/UnitTest1.cs
```

Replace `tests/ProQualEx.ModelSaver.Mvc5.Tests/ProQualEx.ModelSaver.Mvc5.Tests.csproj` with (again retargeted to `net472` after scaffolding):

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.4" />
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../../src/ProQualEx.ModelSaver.Core/ProQualEx.ModelSaver.Core.csproj" />
    <ProjectReference Include="../../src/ProQualEx.ModelSaver.Mvc5/ProQualEx.ModelSaver.Mvc5.csproj" />
  </ItemGroup>

</Project>
```

```bash
dotnet sln ProQualEx.ModelSaver.sln add tests/ProQualEx.ModelSaver.Mvc5.Tests/ProQualEx.ModelSaver.Mvc5.Tests.csproj
```

- [ ] **Step 4: Write the failing tests**

Per the spec, `PersistedFormExtensions.BeginPersistedForm` itself needs a real `ViewContext`/`HttpContextBase` to exercise, which isn't worth mocking — the novel logic is the splice, which is exercised here as a plain, framework-independent function.

Create `tests/ProQualEx.ModelSaver.Mvc5.Tests/FormHtmlSplicerTests.cs`:

```csharp
using System;
using ProQualEx.ModelSaver;
using ProQualEx.ModelSaver.Mvc5.Internal;

namespace ProQualEx.ModelSaver.Mvc5.Tests;

public class FormHtmlSplicerTests
{
    [PersistOnPostback]
    private class OrderModel
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
    }

    [Fact]
    public void Splice_InsertsMissingHiddenFieldsBeforeClosingFormTag()
    {
        var model = new OrderModel { Id = 7, CustomerName = "Ada" };
        var formHtml = "<form action=\"/Order/Save\" method=\"post\"><input name=\"CustomerName\" value=\"Ada\" /></form>";

        var result = FormHtmlSplicer.Splice(formHtml, model);

        var closingTagIndex = result.IndexOf("</form>", StringComparison.Ordinal);
        var hiddenFieldIndex = result.IndexOf("name=\"Id\"", StringComparison.Ordinal);
        Assert.True(hiddenFieldIndex > 0);
        Assert.True(hiddenFieldIndex < closingTagIndex);
    }

    [Fact]
    public void Splice_EverythingAlreadyRendered_LeavesHtmlUnchanged()
    {
        var model = new OrderModel { Id = 7, CustomerName = "Ada" };
        var formHtml = "<form><input name=\"Id\" /><input name=\"CustomerName\" /></form>";

        var result = FormHtmlSplicer.Splice(formHtml, model);

        Assert.Equal(formHtml, result);
    }

    [Fact]
    public void Splice_NoClosingFormTag_AppendsToEnd()
    {
        var model = new OrderModel { Id = 7, CustomerName = "Ada" };
        var formHtml = "<form>";

        var result = FormHtmlSplicer.Splice(formHtml, model);

        Assert.StartsWith("<form>", result, StringComparison.Ordinal);
        Assert.Contains("name=\"Id\"", result);
        Assert.Contains("name=\"CustomerName\"", result);
    }
}
```

- [ ] **Step 5: Run tests, verify they fail to compile**

Run: `dotnet test tests/ProQualEx.ModelSaver.Mvc5.Tests/ProQualEx.ModelSaver.Mvc5.Tests.csproj`

Expected: FAIL — `FormHtmlSplicer` does not exist yet. If this instead fails with `Could not find 'mono' host`, Mono still isn't installed; fall back to `dotnet build tests/ProQualEx.ModelSaver.Mvc5.Tests/ProQualEx.ModelSaver.Mvc5.Tests.csproj` to confirm the compile-time failure instead, and continue using `dotnet build` for verification through the rest of this task, noting clearly that tests were compile-checked only, not executed.

- [ ] **Step 6: Implement `FormHtmlSplicer`**

Create `src/ProQualEx.ModelSaver.Mvc5/Internal/FormHtmlSplicer.cs`:

```csharp
using System;
using ProQualEx.ModelSaver;

namespace ProQualEx.ModelSaver.Mvc5.Internal;

internal static class FormHtmlSplicer
{
    public static string Splice(string formHtml, object model)
    {
        var missingFields = HiddenFieldDiffer.GetMissingFields(model, formHtml);
        var hiddenFieldsHtml = HiddenFieldRenderer.Render(missingFields);

        if (hiddenFieldsHtml.Length == 0)
        {
            return formHtml;
        }

        var closingTagIndex = formHtml.LastIndexOf("</form>", StringComparison.OrdinalIgnoreCase);

        return closingTagIndex < 0
            ? formHtml + hiddenFieldsHtml
            : formHtml.Insert(closingTagIndex, hiddenFieldsHtml);
    }
}
```

- [ ] **Step 7: Run tests, verify PASS (or build, per the Mono fallback)**

Run: `dotnet test tests/ProQualEx.ModelSaver.Mvc5.Tests/ProQualEx.ModelSaver.Mvc5.Tests.csproj`
Expected: all 3 tests pass. If Mono is unavailable, run `dotnet build tests/ProQualEx.ModelSaver.Mvc5.Tests/ProQualEx.ModelSaver.Mvc5.Tests.csproj` instead and expect `Build succeeded.`

- [ ] **Step 8: Implement `PersistedFormScope`**

Create `src/ProQualEx.ModelSaver.Mvc5/Internal/PersistedFormScope.cs`:

```csharp
using System;
using System.IO;
using System.Web.Mvc;

namespace ProQualEx.ModelSaver.Mvc5.Internal;

internal sealed class PersistedFormScope : IDisposable
{
    private readonly ViewContext _viewContext;
    private readonly TextWriter _originalWriter;
    private readonly StringWriter _buffer;
    private readonly MvcForm _innerForm;
    private readonly object _model;
    private bool _disposed;

    public PersistedFormScope(
        ViewContext viewContext,
        TextWriter originalWriter,
        StringWriter buffer,
        MvcForm innerForm,
        object model)
    {
        _viewContext = viewContext;
        _originalWriter = originalWriter;
        _buffer = buffer;
        _innerForm = innerForm;
        _model = model;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _innerForm.Dispose();

        var formHtml = _buffer.ToString();
        var spliced = FormHtmlSplicer.Splice(formHtml, _model);

        _viewContext.Writer = _originalWriter;
        _originalWriter.Write(spliced);
    }
}
```

- [ ] **Step 9: Implement `PersistedFormExtensions`**

Create `src/ProQualEx.ModelSaver.Mvc5/PersistedFormExtensions.cs`:

```csharp
using System;
using System.IO;
using System.Web.Mvc;
using ProQualEx.ModelSaver.Mvc5.Internal;

namespace ProQualEx.ModelSaver.Mvc5;

public static class PersistedFormExtensions
{
    public static IDisposable BeginPersistedForm(
        this HtmlHelper htmlHelper,
        object model,
        string actionName,
        string controllerName,
        FormMethod method = FormMethod.Post,
        object? htmlAttributes = null)
    {
        if (htmlHelper is null)
        {
            throw new ArgumentNullException(nameof(htmlHelper));
        }

        if (model is null)
        {
            throw new ArgumentNullException(nameof(model));
        }

        var viewContext = htmlHelper.ViewContext;
        var originalWriter = viewContext.Writer;
        var buffer = new StringWriter();
        viewContext.Writer = buffer;

        var innerForm = htmlHelper.BeginForm(actionName, controllerName, method, htmlAttributes);

        return new PersistedFormScope(viewContext, originalWriter, buffer, innerForm, model);
    }
}
```

Usage in a real view would look like:

```csharp
@using (Html.BeginPersistedForm(Model, "Save", "Order"))
{
    @Html.TextBoxFor(m => m.CustomerName)
    <button type="submit">Save</button>
}
```

- [ ] **Step 10: Verify the whole solution builds**

Run: `dotnet build ProQualEx.ModelSaver.sln`
Expected: `Build succeeded.` for all five projects (`ProQualEx.ModelSaver.Core`, `ProQualEx.ModelSaver.Core.Tests`, `ProQualEx.ModelSaver.AspNetCore`, `ProQualEx.ModelSaver.AspNetCore.Tests`, `ProQualEx.ModelSaver.Mvc5`, `ProQualEx.ModelSaver.Mvc5.Tests`).

- [ ] **Step 11: Commit**

```bash
git add ProQualEx.ModelSaver.sln src/ProQualEx.ModelSaver.Mvc5 tests/ProQualEx.ModelSaver.Mvc5.Tests
git commit -m "Add ProQualEx.ModelSaver.Mvc5 BeginPersistedForm adapter"
```

---

## Spec coverage check (self-review)

- Attribute API (class default + property override, nested types use their own config): Tasks 2–3. ✅
- Core diff algorithm (eligible paths, name scanning excluding submit/button/reset/image, diff, `CurrentCulture` value formatting, id-from-name): Tasks 3–7. ✅
- ASP.NET Core Tag Helper adapter: Task 8. ✅
- MVC5 `BeginPersistedForm` adapter with buffer-diff-splice: Task 9. ✅
- Package layout (`ProQualEx.ModelSaver.Core`/`ProQualEx.ModelSaver.Mvc5`/`ProQualEx.ModelSaver.AspNetCore`, `src`/`tests` split): Task 1, 8, 9. ✅
- Testing strategy (Core pure xUnit, AspNetCore Tag-Helper-context tests, Mvc5 splice-only tests): Tasks 1–9 throughout. ✅
- Collections/tamper-protection: explicitly out of scope per spec — no task needed. ✅
