# Progress: 07-sites-offbyone

## Files Modified
- `SiteUtilProjects/OnlineVideos.Sites.offbyone/OnlineVideos.Sites.offbyone.csproj` — converted to SDK-style, cleaned up
- `SiteUtilProjects/OnlineVideos.Sites.offbyone/YouTubeV3Util.cs` — replaced `TaskEx.Delay` with `Task.Delay`

## Issues Resolved
- **MSB4064/MSB4063** — `Microsoft.Net.Compilers` 4.0.1 pins an old Roslyn incompatible with the modern SDK `Csc` task. Removed entirely (SDK provides its own Roslyn).
- **Legacy HintPath references** — Replaced HtmlAgilityPack, Newtonsoft.Json, Microsoft.Bcl.Async, Microsoft.Net.Http HintPath refs with clean framework references (flow transitively via ProjectReference or are in-box on net48).
- **Legacy BCL packages** — Removed `Microsoft.Bcl`, `Microsoft.Bcl.Async`, `Microsoft.Bcl.Build`, `Microsoft.Net.Http` (all superseded by .NET 4.8 in-box APIs).
- **CS0103 TaskEx** — `TaskEx.Delay` was from `Microsoft.Bcl.Async` polyfill. Replaced 3 usages with `Task.Delay` (in-box on net48).
- **ClickOnce noise & AllRules.ruleset** — removed stale legacy properties.

## Build Result
✅ 0 errors, 0 warnings (project + full solution)

## packages.config
✅ Removed
