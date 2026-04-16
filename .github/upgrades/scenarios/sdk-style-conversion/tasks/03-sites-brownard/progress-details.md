# Progress: 03-sites-brownard

## Files Modified
- `SiteUtilProjects/OnlineVideos.Sites.brownard/OnlineVideos.Sites.brownard.csproj` — converted to SDK-style
- `OnlineVideos/OnlineVideos.csproj` — added `PrivateAssets=all` to SubtitleDownloader reference

## Issues Resolved
- **CS0433 type ambiguity** — SubtitleDownloader bundles its own HtmlAgilityPack, causing type conflicts when transitively referenced. Fixed by adding `<PrivateAssets>all</PrivateAssets>` to SubtitleDownloader in OnlineVideos.csproj.
- **HintPath references** — Removed stale HintPath refs for HtmlAgilityPack and Newtonsoft.Json (these now flow transitively from the ProjectReference to OnlineVideos).

## Build Result
✅ 0 errors, 0 warnings
