# Assessment: SDK-style Conversion

## Projects to Convert (topological order)

| # | Project | packages.config | AssemblyInfo | Risk |
|---|---------|----------------|--------------|------|
| 1 | OnlineVideos | Yes | Yes | Low |
| 2 | Webview2Installer | No | No | Low |
| 3 | OnlineVideos.Sites.brownard | No | Yes | Low |
| 4 | OnlineVideos.MediaPortal1 | Yes | Yes | Low |
| 5 | SiteParser | No | Yes | Low |
| 6 | OnlineVideos.Sites.doskabouter | No | Yes | Low |
| 7 | OnlineVideos.Sites.offbyone | Yes | Yes | Low |

## Already SDK-style
None.

## Baseline
- Solution builds: **Yes**
- Warnings: 0 errors

## Key Findings
- All projects target `net48`
- 3 projects have `packages.config` (OnlineVideos, OnlineVideos.MediaPortal1, OnlineVideos.Sites.offbyone) — will be migrated to PackageReference
- 6 projects have `AssemblyInfo.cs` — SDK auto-generates these attributes; will need to suppress duplication
- No ASP.NET Framework web projects
- No WPF/WinForms SDK complications
