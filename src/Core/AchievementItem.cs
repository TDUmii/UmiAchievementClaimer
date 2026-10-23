using System;

namespace UmiAchievementClaimer
{
internal sealed class AchievementItem
{
    public string Id;
    public string Name;
    public string Description;
    public bool Achieved;
    public bool English;
    public long UnlockUnix;
    public double GlobalPercent = -1;

    public override string ToString()
    {
        string state = English
            ? (Achieved ? "Unlocked" : "Locked")
            : (Achieved ? "Đã mở" : "Chưa mở");
        return "[" + state + "] " + Name + "  —  " + Id;
    }
}
}
