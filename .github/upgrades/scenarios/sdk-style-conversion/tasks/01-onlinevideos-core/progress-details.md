# Progress: 01-onlinevideos-core

## Files Modified
- `OnlineVideos/OnlineVideos.csproj` — converted to SDK-style

## Issues Resolved
1. **extern alias OVSubs** — SubtitleDownloader PackageReference needed `<Aliases>OVSubs</Aliases>`
2. **MSB3823/MSB3822** — non-string .resx resources required `GenerateResourceUsePreserializedResources=true` + `System.Resources.Extensions` 8.0.0 package
3. **MSB3884** — removed stale `AllRules.ruleset` CodeAnalysisRuleSet references from both configuration PropertyGroups
4. **Noise cleanup** — removed ClickOnce/deploy legacy properties (PublishUrl, Install, InstallFrom, UpdateEnabled, etc.)

## Build Result
✅ 0 errors, 0 warnings

## packages.config
✅ Removed
