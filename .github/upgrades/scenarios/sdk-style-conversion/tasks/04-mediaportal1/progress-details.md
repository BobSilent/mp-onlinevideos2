# Progress: 04-mediaportal1

## Files Modified
- `OnlineVideos.MediaPortal1/OnlineVideos.MediaPortal1.csproj` — converted to SDK-style

## Issues Resolved
- **NETSDK1022 duplicate Compile** — `VlcEventHandler'.cs` had an explicit `<Compile Include>` that conflicted with SDK globbing. Changed to `<Compile Update>`.
- **MSB3823 non-string resources** — added `GenerateResourceUsePreserializedResources=true` (System.Resources.Extensions flows transitively from OnlineVideos project reference).

## Build Result
✅ 0 errors, 0 warnings

## packages.config
✅ Removed
