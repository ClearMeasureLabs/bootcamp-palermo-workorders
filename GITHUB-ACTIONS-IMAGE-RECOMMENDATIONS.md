# GitHub Actions Image Recommendations for .NET 10 and Playwright

## Executive Summary

**There is no GitHub Actions runner image with both .NET 10 and Playwright pre-installed.** However, the current workflow configuration using `ubuntu-latest` with explicit installation steps is already optimal.

## Research Findings (as of February 2026)

### Current GitHub Actions Runner Images

1. **ubuntu-latest**: Points to Ubuntu 24.04
   - **Pre-installed .NET**: Up to .NET 8.x
   - **Pre-installed Playwright**: No
   - **Recommendation**: Continue using with `actions/setup-dotnet@v5` for .NET 10

2. **ubuntu-22.04**: Ubuntu 22.04 LTS
   - **Pre-installed .NET**: Up to .NET 8.x
   - **Pre-installed Playwright**: No
   - **Use case**: Fallback if Ubuntu 24.04 compatibility issues arise

### Why .NET 10 is Not Pre-installed

- .NET 10 is a newer release and has not yet been added to the standard runner images
- Microsoft does not pre-install all .NET versions on GitHub-hosted runners
- The `actions/setup-dotnet` action is the official and recommended approach for specific SDK versions

### Why Playwright is Not Pre-installed

- Playwright requires browser binaries (Chromium, Firefox, WebKit) that are large
- Different projects require different Playwright versions
- Best practice is to install Playwright browsers as part of the workflow

## Current Workflow Analysis

The existing workflows already follow best practices:

### ✅ Optimal Setup in `.github/workflows/integration-build.yml`

```yaml
- name: Setup .NET SDK 10.0.100
  uses: actions/setup-dotnet@v5.0.1
  with:
    dotnet-version: '10.0.100'
```

```powershell
# Playwright installation (lines 535-545)
$playwrightScript = Join-PathSegments "bin" "Release" "net10.0" "playwright.ps1"
& pwsh $playwrightScript install --with-deps
```

**This is the correct and recommended approach.**

## Alternative Options Evaluated

### Option 1: Custom Self-Hosted Runner
- **Pros**: Full control over pre-installed software
- **Cons**: Requires infrastructure maintenance, security updates, and cost
- **Recommendation**: ❌ Not needed for this use case

### Option 2: Playwright Docker Images
- **Image**: `mcr.microsoft.com/playwright/dotnet:v1.58.0-noble`
- **Pros**: Has Playwright and browsers pre-installed
- **Cons**: 
  - Only includes .NET 8 (not .NET 10)
  - Would require containerized workflow setup
  - More complex CI/CD pipeline
- **Recommendation**: ❌ Not suitable for .NET 10 requirement

### Option 3: Ubuntu-latest with Runtime Installation (Current Approach)
- **Image**: `ubuntu-latest` (Ubuntu 24.04)
- **Setup**: 
  - Use `actions/setup-dotnet@v5` to install .NET 10.0.100
  - Build project and install Playwright browsers via `playwright.ps1 install --with-deps`
- **Pros**: 
  - Official and supported approach
  - Fast SDK caching by GitHub Actions
  - Works with any .NET version including previews
  - Minimal overhead (SDKs are cached after first run)
- **Cons**: Small initial setup time (mitigated by caching)
- **Recommendation**: ✅ **This is the best approach** (already implemented)

## Recommendations

### 1. Keep Current Configuration ✅
The existing workflow configuration is optimal and follows GitHub Actions and Playwright best practices.

### 2. No Changes Required
Do not switch runner images or use Docker containers. The current setup:
- Uses the latest Ubuntu runner image
- Installs .NET 10 explicitly (which is the official recommendation)
- Installs Playwright browsers on-demand (which is the official recommendation)

### 3. Monitor for Future Updates
Keep an eye on the [actions/runner-images repository](https://github.com/actions/runner-images) for when .NET 10 might be added to the pre-installed SDKs. Even then, explicit installation is still recommended for version pinning.

### 4. Performance Considerations
- **SDK caching**: `actions/setup-dotnet` caches the SDK, so subsequent runs are fast
- **Browser caching**: Consider adding cache for Playwright browsers if desired:
  ```yaml
  - uses: actions/cache@v4
    with:
      path: ~/.cache/ms-playwright
      key: playwright-${{ runner.os }}-${{ hashFiles('**/AcceptanceTests.csproj') }}
  ```

## Conclusion

**No action required.** The repository already uses the best available GitHub Actions runner image (`ubuntu-latest`) with optimal setup steps for both .NET 10 and Playwright. There is no pre-built image that includes both .NET 10 and Playwright, and creating custom runners or using Docker containers would add unnecessary complexity without meaningful benefit.

## References

- [GitHub Actions Runner Images](https://github.com/actions/runner-images)
- [actions/setup-dotnet](https://github.com/actions/setup-dotnet)
- [Playwright .NET CI Documentation](https://playwright.dev/dotnet/docs/ci)
- [Microsoft Playwright Docker Images](https://mcr.microsoft.com/en-us/product/playwright/dotnet/about)
