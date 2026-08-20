using System;

namespace UmiAchievementClaimer
{
internal sealed class DetectedGame
{
    public string Name;
    public string AppId;
    public string RootDirectory;
    public string SteamApiPath;
    public string SteamApiArchiveEntry;
    public string SteamApiArchiveFormat;
    public long SteamApiArchiveOffset;
    public long SteamApiArchiveLength;
    public string Architecture;
    public string InterfaceVersion;
    public bool UsesCompatibilityProvider;
}
}
