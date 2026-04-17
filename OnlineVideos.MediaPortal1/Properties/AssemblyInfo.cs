using MediaPortal.Common.Utils;

// Define that our plugin is designed for MediaPortal 1.16 Pre - a new Video Renderer was added, so not backward compatible
[assembly: CompatibleVersion("1.15.100.0", "1.15.100.0")]

// Tell MediaPortal which subsystems this plugin will use, so it can check for compatiblity
[assembly: UsesSubsystem("MP.SkinEngine")]
[assembly: UsesSubsystem("MP.Players.Video")]
[assembly: UsesSubsystem("MP.Input")]
[assembly: UsesSubsystem("MP.Externals.SQLite")]
[assembly: UsesSubsystem("MP.Externals.Log4Net")]
[assembly: UsesSubsystem("MP.Config")]
[assembly: UsesSubsystem("MP.Plugins.Videos")]
