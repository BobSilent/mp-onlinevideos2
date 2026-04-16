# SDK-style Conversion Plan

## Overview

**Target**: Convert all 7 legacy `.csproj` projects in the OnlineVideos MediaPortal solution to SDK-style format.
**Scope**: 7 projects, all targeting `net48`. 3 have `packages.config`, 6 have `AssemblyInfo.cs`.

## Tasks

### 01-onlinevideos-core: Convert OnlineVideos (core library)

Convert `OnlineVideos/OnlineVideos.csproj` — the root dependency for all other projects. Has `packages.config` (migrates to PackageReference) and `AssemblyInfo.cs` (suppress auto-generation). Must succeed before any downstream conversion can proceed.

**Done when**: Project builds successfully in SDK-style format, `packages.config` removed.

---

### 02-webview2installer: Convert Webview2Installer

Convert `MPEI/Webview2/Webview2Installer.csproj`. No `packages.config`, no `AssemblyInfo.cs` — straightforward conversion.

**Done when**: Project builds successfully in SDK-style format.

---

### 03-sites-brownard: Convert OnlineVideos.Sites.brownard

Convert `SiteUtilProjects/OnlineVideos.Sites.brownard/OnlineVideos.Sites.brownard.csproj`. No `packages.config`, has `AssemblyInfo.cs`.

**Done when**: Project builds successfully in SDK-style format.

---

### 04-mediaportal1: Convert OnlineVideos.MediaPortal1

Convert `OnlineVideos.MediaPortal1/OnlineVideos.MediaPortal1.csproj`. Has `packages.config` and `AssemblyInfo.cs`. Depends on OnlineVideos core.

**Done when**: Project builds successfully in SDK-style format, `packages.config` removed.

---

### 05-siteparser: Convert SiteParser

Convert `SiteParser/SiteParser.csproj`. No `packages.config`, has `AssemblyInfo.cs`.

**Done when**: Project builds successfully in SDK-style format.

---

### 06-sites-doskabouter: Convert OnlineVideos.Sites.doskabouter

Convert `SiteUtilProjects/OnlineVideos.Sites.doskabouter/OnlineVideos.Sites.doskabouter.csproj`. No `packages.config`, has `AssemblyInfo.cs`.

**Done when**: Project builds successfully in SDK-style format.

---

### 07-sites-offbyone: Convert OnlineVideos.Sites.offbyone

Convert `SiteUtilProjects/OnlineVideos.Sites.offbyone/OnlineVideos.Sites.offbyone.csproj`. Has `packages.config` and `AssemblyInfo.cs`.

**Done when**: Project builds successfully in SDK-style format, `packages.config` removed.
