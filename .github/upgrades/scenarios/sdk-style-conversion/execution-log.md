
## [2026-04-16 14:16] 01-onlinevideos-core

Converted OnlineVideos/OnlineVideos.csproj to SDK-style. Fixed: SubtitleDownloader extern alias, non-string .resx resources (GenerateResourceUsePreserializedResources + System.Resources.Extensions), removed stale ruleset references and ClickOnce noise. packages.config removed. 0 errors, 0 warnings.


## [2026-04-16 14:17] 02-webview2installer

Converted MPEI/Webview2/Webview2Installer.csproj to SDK-style. Clean conversion, no issues. 0 errors, 0 warnings.


## [2026-04-16 14:34] 03-sites-brownard

Converted OnlineVideos.Sites.brownard to SDK-style. Fixed CS0433 type ambiguity from SubtitleDownloader's bundled HtmlAgilityPack by adding PrivateAssets=all to SubtitleDownloader in OnlineVideos.csproj. Removed stale HintPath refs for HtmlAgilityPack and Newtonsoft.Json. 0 errors, 0 warnings.


## [2026-04-16 14:39] 04-mediaportal1

Converted OnlineVideos.MediaPortal1 to SDK-style. Fixed NETSDK1022 duplicate Compile (VlcEventHandler'.cs Include→Update) and MSB3823 non-string resources (GenerateResourceUsePreserializedResources=true). packages.config removed. 0 errors, 0 warnings.


## [2026-04-16 14:41] 05-siteparser

Converted SiteParser to SDK-style. Fixed MSB3823 non-string resources with GenerateResourceUsePreserializedResources=true. 0 errors, 0 warnings.


## [2026-04-16 14:43] 06-sites-doskabouter

Converted OnlineVideos.Sites.doskabouter to SDK-style. Clean conversion. 0 errors, 5 pre-existing CS8632 nullable warnings (not introduced by conversion).


## [2026-04-16 15:13] 07-sites-offbyone

Converted OnlineVideos.Sites.offbyone to SDK-style. Removed Microsoft.Net.Compilers (old Roslyn incompatible with SDK Csc task), replaced legacy HintPath/BCL package refs with in-box net48 equivalents, fixed 3x TaskEx.Delay → Task.Delay in YouTubeV3Util.cs. packages.config removed. Full solution builds with 0 errors, 0 warnings.

