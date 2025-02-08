using System.Collections.Generic;

namespace ClassicFallout4.Common.Constants;

/// <summary>
/// Core constants used throughout the CLASSIC application
/// </summary>
public static class ClassicConstants
{
    // Auto backup list
    public static readonly IReadOnlyList<string> AutoBackupFiles =
    [
        "Fallout4.exe",
        "Fallout4VR.exe",
        "Fallout4Launcher.exe",
        "Fallout4VRLauncher.exe"
    ];

    // Log error detection strings
    public static readonly IReadOnlyList<string> LogErrorPatterns =
    [
        "critical",
        "error", 
        "failed"
    ];

    // Log record detection strings
    public static readonly IReadOnlyList<string> LogRecordPatterns =
    [
        ".bgsm",
        ".bto",
        ".btr",
        ".dds",
        ".dll+",
        ".fuz",
        ".hkb",
        ".hkx",
        ".ini",
        ".nif",
        ".pex",
        ".strings",
        ".swf",
        ".txt",
        ".uvd",
        ".wav",
        ".xwm",
        "data/",
        "data\\",
        "scaleform",
        "editorid:",
        "file:",
        "function:",
        "name:"
    ];

    // Log record exclusions
    public static readonly IReadOnlyList<string> ExcludedLogRecords =
    [
        "(Main*)",
        "(size_t)",
        "(void*)",
        "Basic Render Driver"
    ];

    // Log error exclusions
    public static readonly IReadOnlyList<string> ExcludedLogErrors =
    [
        "failed to get next record",
        "failed to open pdb",
        "failed to register method",
        "keybind",
        "no errors with this",
        "unable to locate pdb"
    ];

    // Log file exclusions
    public static readonly IReadOnlyList<string> ExcludedLogFiles =
    [
        "cbpfo4",
        "crash-",
        "CreationKit",
        "DumpStack",
        "dxgi",
        "f4se",
        "HHS",
        "PluginPreloader"
    ];

    // Game hints
    public static readonly IReadOnlyList<string> GameHints =
    [
        "Random Hint: [Ctrl] + [F] is a handy-dandy key combination. You should use it more often. Please.",
        "Random Hint: Patrolling the Buffout 4 Nexus Page almost makes you wish this joke was more overused.",
        "Random Hint: Buffout crash logs are generated in your Documents/My Games/Fallout4/F4SE(VR) folder by default.",
        "Random Hint: You have a crash log where the auto scan could not find any solutions? Feel free to send it to me.",
        "Random Hint: 20% of all crashes are caused by Classic Holstered Weapons mod. 80% of all statistics are made up.",
        "Random Hint: If you do not use Profile Specific INIs, consider disabling this option in your MO2 Profile settings.", 
        "Random Hint: Spending 5 morbillion hours asking for help can save you from 5 minutes of reading the documentation.",
        "Random Hint: When necessary, make sure that crashes are consistent or repeatable, since in rare cases they are not.",
        "Random Hint: Revisit both Buffout 4 Crash Article and CLASSIC Nexus Page from time to time in case there are updates.",
        "Random Hint: When posting crash logs, it is helpful to mention the last thing you were doing before the crash happened."
    ];
}

/// <summary>
/// Constants related to default configuration templates and settings
/// </summary>
public static class ConfigurationConstants
{
    // Default custom INI settings
    public const string DefaultCustomIni = @"[Archive]
; Enable Archive Invalidation
bInvalidateOlderFiles=1
sResourceDataDirsFinal=

[Display]
; Increase Pip-Boy Resolution
uPipboyTargetHeight=1400
uPipboyTargetWidth=1752
; Lower PA Screen Brightness
fPipboyScreenEmitIntensityPA=1.25
fPipboyScreenDiffuseIntensityPA=0.15
; Center The Game Window
iLocation X=0
iLocation Y=0
; Increase Shadow Quality Transition Distance
fBlendSplitDirShadow=256";

    // Default settings template
    public const string DefaultSettings = @"# This file contains settings for CLASSIC v7.00+, used by both source scripts and the executable.

CLASSIC_Settings:

# Set the game that you want CLASSIC to currently manage. (Fallout 4 | Skyrim SE | Starfield)
  Managed Game: Fallout 4

# Set to true if you want CLASSIC to periodically check for its own updates online through GitHub.
  Update Check: true

# Set to true if you want CLASSIC to prioritize scanning the Virtual Reality version of your game.
  VR Mode: false

# FCX - File Check Xtended | Set to true if you want CLASSIC to check the integrity of your game files and core mods.
  FCX Mode: true

# Set to true if you want CLASSIC to remove some unnecessary lines and redundant information from your crash log files.  
# CAUTION: Changes will be permanent for each crash log you scan after. May hide info useful for debugger programs.
  Simplify Logs: false

# Set to true if you want CLASSIC to show extra stats about scanned logs in the command line / terminal window.
# NOTICE: This setting currently has no effect, crash log stats will be fully implemented in a future update.
  Show Statistics: false 

# Set to true if you want CLASSIC to look up FormID values (names) automatically while scanning crash logs.
# This will show some extra details for Possible FormID Suspects at the expense of longer scanning times.
  Show FormID Values: false

# Set to true if you want CLASSIC to move all unsolved crash logs and their autoscans to CLASSIC UNSOLVED folder.
# Unsolved logs are all crash logs that are incomplete or in the wrong format.
  Move Unsolved Logs: true

# Copy-paste your INI folder path below, where your main game INI files are located (Documents\\My Games\\*game*)
# If you are using MO2, I recommend disabling Profile Specific Game INI Files, located in Tools > Profiles
# This is only required if CLASSIC has problems detecting your game files or is scanning the wrong game.
  INI Folder Path:

# Copy-paste your staging mods folder path below. (Folder where your mod manager keeps all extracted mod files).
# MO2 Ex. MODS Folder Path: C:\\Mod Organizer 2\\*game*\\mods | Vortex Ex. MODS Folder Path: C:\\Vortex Mods\\*game*
# You can also set this path to your game's Data folder, but then the scan results will be much less accurate.
  MODS Folder Path:

# Copy-paste your custom crash logs folder path below. Ex. SCAN Custom Path: C:\\My Crash Logs
# Crash logs are generated in Documents\\My Games\\*game*\\XSE folder by default. If no path is set,
# crash logs from that Scrip Extender folder and where the CLASSIC.exe is located will be scanned.
  SCAN Custom Path:

# Toggle audio notifications for when CLASSIC finishes scanning your crash logs and mod files.
  Audio Notifications: true

# Set the source where CLASSIC will check for updates. (Nexus | GitHub)
  Update Source: Both";
}

/// <summary>
/// Constants related to warning messages and user notifications
/// </summary>
public static class MessageConstants 
{
    public static class GameWarnings
    {
        public const string RootPath = @"❌ CAUTION : YOUR GAME FILES ARE INSTALLED INSIDE OF THE DEFAULT PROGRAM FILES FOLDER!
      Having the game installed here may cause Windows UAC to prevent some mods from working correctly.
      To ensure that everything works, move your Game or entire Steam folder outside of Program Files.
    -----";

        public const string DocsPath = @"❌ CAUTION : MICROSOFT ONEDRIVE IS OVERRIDING YOUR DOCUMENTS FOLDER PATH!
      This can sometimes cause various save file and file permissions problems.
      To avoid this, disable Documents folder backup in your OneDrive settings.
    -----";
    }

    public static class ModWarnings
    {
        public const string ModsPathInvalid = @"❌ ERROR : YOUR MODS FOLDER PATH IS INVALID! PLEASE OPEN *CLASSIC Settings.yaml*
    AND ENTER A VALID FOLDER PATH FOR *MODS Folder Path* FROM YOUR MOD MANAGER.
    -----";

        public const string ModsPathMissing = @"❌ MODS FOLDER PATH NOT PROVIDED! TO SCAN ALL YOUR MOD FILES, PLEASE OPEN
    *CLASSIC Settings.yaml* AND ENTER A FOLDER PATH FOR *MODS Folder Path*.
    -----";

        public const string ModsBsArchMissing = @"❌ BSARCH EXECUTABLE CANNOT BE FOUND. TO SCAN ALL YOUR MOD ARCHIVES, PLEASE DOWNLOAD
    THE LATEST VERSION OF BSARCH AND EXTRACT ITS EXE INTO THE *CLASSIC Data* FOLDER
    BSArch Link: https://www.nexusmods.com/newvegas/mods/64745?tab=files
    -----";

        public const string ModsPluginLimit = @"# [!] CAUTION : ONE OF YOUR PLUGINS HAS THE [FF] PLUGIN INDEX VALUE #
    * THIS MEANS YOU ALMOST CERTAINLY WENT OVER THE GAME PLUGIN LIMIT! *
    Disable some of your esm/esp plugins and re-run the Crash Logs Scan.
    -----";
    }
}

/// <summary>
/// Constants related to the user interface and application display
/// </summary>
public static class InterfaceConstants
{
    public const string StartMessage = @"PRESS THE *SCAN CRASH LOGS* BUTTON TO SCAN ALL AVAILABLE CRASH LOGS

PRESS THE *SCAN GAME FILES* BUTTON TO SCAN YOUR GAME & MOD FILES

IF YOU ARE USING MOD ORGANIZER 2, RUN CLASSIC WITH THE MO2 SHORTCUT
READ THE *CLASSIC Readme.pdf* FILE FOR MORE DETAILS AND INSTRUCTIONS";

    public const string UpdatePopupText = @"New CLASSIC version is available! Press OK to open the CLASSIC Unofficial GitHub Page.

CLASSIC Unofficial : https://www.github.com/evildarkarchon/CLASSIC-Fallout4/releases/latest";

    public const string UpdateWarningFallout4 = @"❌ WARNING : YOUR FALLOUT 4 CLASSIC VERSION IS OUT OF DATE!
YOU CAN GET THE LATEST FALLOUT 4 CLASSIC VERSION FROM HERE:
https://www.nexusmods.com/fallout4/mods/56255";

    public const string UpdateUnableFallout4 = @"❌ WARNING : CLASSIC WAS UNABLE TO CHECK FOR UPDATES AT THIS TIME, TRY AGAIN LATER
CHECK FOR NEW CLASSIC VERSIONS HERE: https://www.nexusmods.com/fallout4/mods/56255";

    public const string AutoscanTextFallout4 = @"FOR FULL LIST OF MODS THAT CAUSE PROBLEMS, THEIR ALTERNATIVES AND DETAILED SOLUTIONS
VISIT THE BUFFOUT 4 CRASH ARTICLE: https://www.nexusmods.com/fallout4/articles/3115
===============================================================================
Author/Made By: Poet (guidance.of.grace) | https://discord.gg/DfFYJtt8p4
CONTRIBUTORS | evildarkarchon | kittivelae | AtomicFallout757 | wxMichael
FO4 CLASSIC | https://www.nexusmods.com/fallout4/mods/56255";
}