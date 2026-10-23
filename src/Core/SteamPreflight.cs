using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace UmiAchievementClaimer
{
    // Local, read-only preflight. A positive result cannot guarantee game-specific Steamworks compatibility.
    internal static class SteamPreflight
    {
        internal static List<string> Inspect(DetectedGame game, bool steamRunning)
        {
            var messages = new List<string>();
            messages.Add((steamRunning ? "[OK] " : "[!] ") +
                (steamRunning ? "Steam process detected." : "Steam is not running. Start Steam in Online mode."));
            if (game == null)
            {
                messages.Add("[!] No game detected. Choose a game and scan its installation.");
                return messages;
            }
            messages.Add(Directory.Exists(game.RootDirectory)
                ? "[OK] Game directory exists." : "[!] Game directory no longer exists.");
            messages.Add(!string.IsNullOrWhiteSpace(game.AppId) && Regex.IsMatch(game.AppId, @"^\d+$")
                ? "[OK] Valid numeric Steam AppID: " + game.AppId : "[!] Steam AppID is invalid.");
            messages.Add(File.Exists(game.SteamApiPath)
                ? "[OK] Steam API source exists." : "[!] Steam API source is missing.");
            messages.Add(game.Architecture == "x64" || game.Architecture == "x86"
                ? "[OK] Worker architecture: " + game.Architecture : "[!] Unsupported worker architecture.");
            messages.Add(game.UsesCompatibilityProvider
                ? "[!] Compatibility provider in use; game support is not guaranteed."
                : "[i] Game-specific claim support must still be checked in a live Steam session.");
            messages.Add("[i] A preflight never submits or changes achievements.");
            return messages;
        }
    }
}
