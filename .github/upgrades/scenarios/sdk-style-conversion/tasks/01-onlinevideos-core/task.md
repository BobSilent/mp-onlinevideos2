# 01-onlinevideos-core: Convert OnlineVideos (core library)

Convert `OnlineVideos/OnlineVideos.csproj` — the root dependency for all other projects. Has `packages.config` (migrates to PackageReference) and `AssemblyInfo.cs` (suppress auto-generation). Must succeed before any downstream conversion can proceed.

**Done when**: Project builds successfully in SDK-style format, `packages.config` removed.
