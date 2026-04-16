# SDK-style Conversion Progress

## Overview

Converting 7 legacy csproj projects in the OnlineVideos MediaPortal solution to SDK-style format. All projects target .NET Framework 4.8 — no framework changes. Projects with packages.config will have NuGet references migrated to PackageReference.

**Progress**: 2/7 tasks complete <progress value="29" max="100"></progress> 29%

## Tasks

- ✅ 01-onlinevideos-core: Convert OnlineVideos (core library) ([Content](tasks/01-onlinevideos-core/task.md), [Progress](tasks/01-onlinevideos-core/progress-details.md))
- ✅ 02-webview2installer: Convert Webview2Installer ([Content](tasks/02-webview2installer/task.md), [Progress](tasks/02-webview2installer/progress-details.md))
- 🔄 03-sites-brownard: Convert OnlineVideos.Sites.brownard ([Content](tasks/03-sites-brownard/task.md))
- 🔲 04-mediaportal1: Convert OnlineVideos.MediaPortal1
- 🔲 05-siteparser: Convert SiteParser
- 🔲 06-sites-doskabouter: Convert OnlineVideos.Sites.doskabouter
- 🔲 07-sites-offbyone: Convert OnlineVideos.Sites.offbyone
