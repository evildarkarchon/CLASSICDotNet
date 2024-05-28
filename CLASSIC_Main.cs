using System.IO.Compression;
using System.IO;
using System.Data.SQLite;
using System.Net.Http;
using System.Text.Json;
using System.Security.Cryptography;
using NLog;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace CLASSICDotNet
{
    public class DocsChecker
    {
        public static string DocsCheckFolder()
        {
            var messageList = new List<string>();
            string docsName = YAMLData.CLASSIC_Main.ReadOrUpdateEntry<string>($"Game{Globals.Vr}_Info.Main_Docs_Name");

            if (docsName.ToLower().Contains("onedrive"))
            {
                string docsWarn = YAMLData.CLASSIC_Main.ReadOrUpdateEntry<string>("Warnings_GAME.warn_docs_path");
                messageList.Add(docsWarn);
            }

            return string.Join("", messageList);
        }

        public static string DocsCheckIni(string iniName)
        {
            var messageList = new List<string>();
            Console.WriteLine($"- - - INITIATED {iniName} CHECK");

            string folderDocs = YAMLData.CLASSIC_Main.ReadOrUpdateEntry<string>($"Game{Globals.Vr}_Info.Root_Folder_Docs");
            string docsName = YAMLData.CLASSIC_Main.ReadOrUpdateEntry<string>($"Game{Globals.Vr}_Info.Main_Docs_Name");

            var iniFileList = Directory.GetFiles(folderDocs, "*.ini").Select(Path.GetFileName).ToList();
            var iniPath = Path.Combine(folderDocs, iniName);

            if (iniFileList.Any(file => file.Equals(iniName, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    RemoveReadOnly(iniPath);

                    var configurationBuilder = new ConfigurationBuilder()
                        .SetBasePath(folderDocs)
                        .AddIniFile(iniName, optional: false, reloadOnChange: false);
                    IConfigurationRoot configuration = configurationBuilder.Build();

                    messageList.Add($"✔️ No obvious corruption detected in {iniName}, file seems OK! \n-----\n");

                    if (iniName.Equals($"{docsName}Custom.ini", StringComparison.OrdinalIgnoreCase))
                    {
                        var section = configuration.GetSection("Archive");
                        if (!section.Exists())
                        {
                            messageList.AddRange(new[]
                            {
                            "❌ WARNING : Archive Invalidation / Loose Files setting is not enabled. \n",
                            "  CLASSIC will now enable this setting automatically in the game INI files. \n-----\n"
                        });

                            // Adding the section and keys
                            var iniData = new Dictionary<string, string>
                        {
                            { "Archive:bInvalidateOlderFiles", "1" },
                            { "Archive:sResourceDataDirsFinal", "" }
                        };
                            SaveIniFile(iniPath, iniData);
                        }
                        else
                        {
                            messageList.Add("✔️ Archive Invalidation / Loose Files setting is already enabled! \n-----\n");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    messageList.AddRange(new[]
                    {
                    $"[!] CAUTION : YOUR {iniName} FILE IS SET TO READ ONLY. \n",
                    "     PLEASE REMOVE THE READ ONLY PROPERTY FROM THIS FILE, \n",
                    "     SO CLASSIC CAN MAKE THE REQUIRED CHANGES TO IT. \n-----\n"
                });
                }
                catch (Exception ex)
                {
                    messageList.AddRange(new[]
                    {
                    $"[!] CAUTION : YOUR {iniName} FILE IS VERY LIKELY BROKEN, PLEASE CREATE A NEW ONE \n",
                    $"    Delete this file from your Documents/My Games/{docsName} folder, then press \n",
                    $"    *Scan Game Files* in CLASSIC to generate a new {iniName} file. \n-----\n"
                });
                }
            }
            else
            {
                if (iniName.Equals($"{docsName}.ini", StringComparison.OrdinalIgnoreCase))
                {
                    messageList.AddRange(new[]
                    {
                    $"❌ CAUTION : {iniName} FILE IS MISSING FROM YOUR DOCUMENTS FOLDER! \n",
                    $"   You need to run the game at least once with {docsName}Launcher.exe \n",
                    "    This will create files and INI settings required for the game to run. \n-----\n"
                });
                }

                if (iniName.Equals($"{docsName}Custom.ini", StringComparison.OrdinalIgnoreCase))
                {
                    using (var iniFile = new StreamWriter(iniPath, false, Encoding.UTF8))
                    {
                        messageList.AddRange(new[]
                        {
                        "❌ WARNING : Archive Invalidation / Loose Files setting is not enabled. \n",
                        "  CLASSIC will now enable this setting automatically in the game INI files. \n-----\n"
                    });

                        var customIniConfig = YAMLData.CLASSIC_Main.ReadOrUpdateEntry<string>("Default_CustomINI");
                        iniFile.Write(customIniConfig);
                    }
                }
            }

            return string.Join("", messageList);
        }

        private static void RemoveReadOnly(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.IsReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }
        }

        private static void SaveIniFile(string iniPath, IDictionary<string, string> data)
        {
            var iniContent = new StringBuilder();
            foreach (var kvp in data)
            {
                var sectionKey = kvp.Key.Split(':');
                if (sectionKey.Length == 2)
                {
                    iniContent.AppendLine($"[{sectionKey[0]}]");
                    iniContent.AppendLine($"{sectionKey[1]}={kvp.Value}");
                }
            }
            File.WriteAllText(iniPath, iniContent.ToString(), Encoding.UTF8);
        }
    }

    public class XseIntegrityChecker
    {
        public static string XseCheckHashes()
        {
            var messageList = new List<string>();
            Console.WriteLine("- - - INITIATED XSE FILE HASH CHECK");

            bool xseScriptMissing = false;
            bool xseScriptMismatch = false;

            Dictionary<string, string> xseHashedScripts = YAMLData.CLASSIC_Main.ReadOrUpdateEntry<Dictionary<string, string>>($"Game{Globals.Vr}_Info.XSE_HashedScripts");
            var gameFolderScripts = YAMLData.CLASSIC_Main.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Game_Folder_Scripts");

            var xseHashedScriptsLocal = new Dictionary<string, string>();

            foreach (var key in xseHashedScripts.Keys)
            {
                var scriptPath = Path.Combine(gameFolderScripts, key);
                if (File.Exists(scriptPath))
                {
                    using (var sha256 = SHA256.Create())
                    {
                        using (var stream = File.OpenRead(scriptPath))
                        {
                            var fileHash = sha256.ComputeHash(stream);
                            xseHashedScriptsLocal[key] = BitConverter.ToString(fileHash).Replace("-", "").ToLowerInvariant();
                        }
                    }
                }
            }

            foreach (var key in xseHashedScripts.Keys)
            {
                if (xseHashedScriptsLocal.ContainsKey(key))
                {
                    var hash1 = xseHashedScripts[key];
                    var hash2 = xseHashedScriptsLocal[key];
                    if (hash1 == hash2)
                    {
                        // Hashes match, do nothing
                    }
                    else if (hash2 == null)
                    {
                        messageList.Add($"❌ CAUTION : {key} Script Extender file is missing from your game Scripts folder! \n-----\n");
                        xseScriptMissing = true;
                    }
                    else
                    {
                        messageList.Add($"[!] CAUTION : {key} Script Extender file is outdated or overridden by another mod! \n-----\n");
                        xseScriptMismatch = true;
                    }
                }
            }

            if (xseScriptMissing)
            {
                var warnMissing = YAMLData.CLASSIC_Main.ReadOrUpdateEntry("Warnings_XSE.Warn_Missing");
                messageList.Add(warnMissing);
            }
            if (xseScriptMismatch)
            {
                var warnMismatch = YAMLData.CLASSIC_Main.ReadOrUpdateEntry("Warnings_XSE.Warn_Mismatch");
                messageList.Add(warnMismatch);
            }
            if (!xseScriptMissing && !xseScriptMismatch)
            {
                messageList.Add("✔️ All Script Extender files have been found and accounted for! \n-----\n");
            }

            return string.Join("", messageList);
        }
        public static string XseCheckIntegrity()
        {
            var failedList = new List<string>();
            var messageList = new List<string>();
            Console.WriteLine("- - - INITIATED XSE INTEGRITY CHECK");

            string? catchErrors = YAMLData.CLASSIC_Main.ReadOrUpdateEntry("catch_log_errors");
            string? xseAcronym = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.XSE_Acronym");
            string? xseLogFile = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Docs_File_XSE");
            string? xseFullName = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.XSE_FullName");
            string? xseVerLatest = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.XSE_Ver_Latest");
            string? adlibFile = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Game_File_AddressLib");

            if (File.Exists(adlibFile) && !string.IsNullOrEmpty(adlibFile))
            {
                messageList.Add("✔️ REQUIRED: *Address Library* for Script Extender is installed! \n-----\n");
            }
            else
            {
                string warnAdlibMissing = YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry("Warnings_MODS.Warn_ADLIB_Missing");
                messageList.Add(warnAdlibMissing);
            }

            if (File.Exists(xseLogFile) && !string.IsNullOrEmpty(xseLogFile))
            {
                messageList.Add($"✔️ REQUIRED: *{xseFullName}* is installed! \n-----\n");
                var xseData = File.ReadAllLines(xseLogFile);

                if (xseData.Length > 0 && xseData[0].Contains(xseVerLatest))
                {
                    messageList.Add($"✔️ You have the latest version of *{xseFullName}*! \n-----\n");
                }
                else
                {
                    string warnXseOutdated = YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry("Warnings_XSE.Warn_Outdated");
                    messageList.Add(warnXseOutdated);
                }

                foreach (var line in xseData)
                {
                    if (catchErrors.Split(',').Any(item => line.Contains(item, StringComparison.OrdinalIgnoreCase)))
                    {
                        failedList.Add(line);
                    }
                }

                if (failedList.Count > 0)
                {
                    messageList.Add($"#❌ CAUTION : {xseAcronym}.log REPORTS THE FOLLOWING ERRORS #\n");
                    foreach (var elem in failedList)
                    {
                        messageList.Add($"ERROR > {elem.Trim()} \n-----\n");
                    }
                }
            }
            else
            {
                messageList.Add($"❌ CAUTION : *{xseAcronym.ToLower()}.log* FILE IS MISSING FROM YOUR DOCUMENTS FOLDER! \n");
                messageList.Add($"   You need to run the game at least once with {xseAcronym.ToLower()}_loader.exe \n");
                messageList.Add("    After that, try running CLASSIC again! \n-----\n");
            }
            List<string?> output = new List<string?> { messageList.ToString(), failedList.ToString() };
            return string.Join("", messageList);
        }
    }
    public class GameIntegrityChecker
    {
        public static string GameCheckIntegrity()
        {
            var messageList = new List<string>();
            Console.WriteLine("- - - INITIATED GAME INTEGRITY CHECK");

            string? steamIniLocal = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Game_File_SteamINI");
            string? exeHashOld = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.EXE_HashedOLD");
            string? gameExeLocal = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Game_File_EXE");
            string? rootName = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Main_Root_Name");

#pragma warning disable CS8604 // Possible null reference argument.
            var gameExePath = new FileInfo(gameExeLocal);
            var steamIniPath = new FileInfo(steamIniLocal);
#pragma warning restore CS8604 // Possible null reference argument.
            if (gameExePath.Exists)
            {
                string exeHashLocal;
                using (var sha256 = SHA256.Create())
                {
                    using (var stream = gameExePath.OpenRead())
                    {
                        var fileContents = sha256.ComputeHash(stream);
                        exeHashLocal = BitConverter.ToString(fileContents).Replace("-", "").ToLowerInvariant();
                    }
                }

                if (exeHashLocal == exeHashOld && !steamIniPath.Exists)
                {
                    messageList.Add($"✔️ You have the latest version of {rootName}! \n-----\n");
                }
                else if (steamIniPath.Exists)
                {
                    messageList.Add($"❌ CAUTION : YOUR {rootName} GAME / EXE VERSION IS OUT OF DATE \n-----\n");
                }
                else
                {
                    messageList.Add($"❌ CAUTION : YOUR {rootName} GAME / EXE VERSION IS OUT OF DATE \n-----\n");
                }

                if (!gameExePath.FullName.Contains("Program Files"))
                {
                    messageList.Add($"✔️ Your {rootName} game files are installed outside of the Program Files folder! \n-----\n");
                }
                else
                {
                    string? rootWarn = YAMLData.CLASSIC_Main.ReadOrUpdateEntry("Warnings_GAME.warn_root_path");
#pragma warning disable CS8604 // Possible null reference argument.
                    messageList.Add(rootWarn);
#pragma warning restore CS8604 // Possible null reference argument.
                }
            }

            return string.Join("", messageList);
        }
    }

    public class GamePathGenerator
    {
        public static void GameGeneratePaths()
        {
            Console.WriteLine("- - - INITIATED GAME PATH GENERATION");

            string? gamePath = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Root_Folder_Game");
            string? xseAcronym = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.XSE_Acronym");
            string? xseAcronymBase = YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game_Info.XSE_Acronym");

            YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Game_Folder_Data", Path.Combine(gamePath, "Data"));
            YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Game_Folder_Scripts", Path.Combine(gamePath, "Data", "Scripts"));
            YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Game_Folder_Plugins", Path.Combine(gamePath, "Data", xseAcronymBase, "Plugins"));
            YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Game_File_SteamINI", Path.Combine(gamePath, "steam_api.ini"));
            YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Game_File_EXE", Path.Combine(gamePath, $"{Globals.Game}{Globals.Vr}.exe"));

            if (Globals.Game == "Fallout4")
            {
                if (string.IsNullOrEmpty(Globals.Vr))
                {
                    YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry("Game_Info.Game_File_AddressLib", Path.Combine(gamePath, "Data", xseAcronymBase, "plugins", "version-1-10-163-0.bin"));
                }
                else
                {
                    YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry("GameVR_Info.Game_File_AddressLib", Path.Combine(gamePath, "Data", xseAcronymBase, "plugins", "version-1-2-72-0.csv"));
                }
            }
        }
    }

    public class DocsPathFinder
    {
        private static string game = "Fallout4"; // Example game name
        private static string vr = ""; // Example VR mode check

        public static void DocsPathFind()
        {
            Console.WriteLine("- - - INITIATED DOCS PATH CHECK");

            string? gameSid = YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Main_SteamID");
            string? docsName = YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Main_Docs_Name");

            string? GetWindowsDocsPath()
            {
                const int CSIDL_PERSONAL = 5;
                const int SHGFP_TYPE_CURRENT = 0;
                var path = new char[260];
                if (SHGetFolderPath(IntPtr.Zero, CSIDL_PERSONAL, IntPtr.Zero, SHGFP_TYPE_CURRENT, path) == 0)
                {
                    var winDocs = Path.Combine(new string(path).TrimEnd('\0'), $"My Games\\{docsName}");
                    YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Root_Folder_Docs", winDocs);
                    return winDocs;
                }
                return null;
            }

            void GetLinuxDocsPath()
            {
                var libraryFoldersPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/Steam/steamapps/common/libraryfolders.vdf");
                if (File.Exists(libraryFoldersPath))
                {
                    var lines = File.ReadAllLines(libraryFoldersPath);
                    string libraryPath = "";
                    foreach (var line in lines)
                    {
                        if (line.Contains("\"path\""))
                            libraryPath = line.Split('"')[3];
                        if (line.Contains(gameSid))
                        {
                            var linuxDocs = Path.Combine(libraryPath, "steamapps/compatdata", gameSid, "pfx/drive_c/users/steamuser/My Documents/My Games", docsName);
                            YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Root_Folder_Docs", linuxDocs);
                            break;
                        }
                    }
                }
            }

            void GetManualDocsPath()
            {
                Console.WriteLine($"> > > PLEASE ENTER THE FULL DIRECTORY PATH WHERE YOUR {docsName}.ini IS LOCATED < < <");
                while (true)
                {
                    Console.Write($"(EXAMPLE: C:/Users/Zen/Documents/My Games/{docsName} | Press ENTER to confirm.)\n> ");
                    var pathInput = Console.ReadLine();
                    if (Directory.Exists(pathInput))
                    {
                        Console.WriteLine($"You entered: '{pathInput}' | This path will be automatically added to CLASSIC Settings.yaml");
                        YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Root_Folder_Docs", pathInput.Trim());
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"'{pathInput}' is not a valid or existing directory path. Please try again.");
                    }
                }
            }

            string? docsPath = YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Root_Folder_Docs");
            if (string.IsNullOrEmpty(docsPath))
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    docsPath = GetWindowsDocsPath();
                }
                else
                {
                    GetLinuxDocsPath();
                }
            }

            if (!Directory.Exists(docsPath))
            {
                GetManualDocsPath();
            }
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwFlags, [Out] char[] lpszPath);
    }

    public class DocsPathGenerator
    {
        public static void DocsGeneratePaths()
        {
            Console.WriteLine("- - - INITIATED DOCS PATH GENERATION");

            string? xseAcronym = YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.XSE_Acronym");
            string? xseAcronymBase = YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry($"Game_Info.XSE_Acronym");
            string? docsPath = YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Root_Folder_Docs");


#pragma warning disable CS8604 // Possible null reference argument.
            YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Docs_Folder_XSE", Path.Combine(docsPath, xseAcronymBase));
#pragma warning restore CS8604 // Possible null reference argument.

            YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Docs_File_PapyrusLog", Path.Combine(docsPath, "Logs", "Script", "Papyrus.0.log"));
            YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Docs_File_WryeBashPC", Path.Combine(docsPath, "ModChecker.html"));
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            YAMLData.CLASSIC_Fallout4_Local.ReadOrUpdateEntry($"Game{Globals.Vr}_Info.Docs_File_XSE", Path.Combine(docsPath, xseAcronymBase, $"{xseAcronym.ToLower()}.log"));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
        }
    }

    public class YamlCache
    {
        private readonly string? _filePath;
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        private readonly IDeserializer _deserializer;
        private readonly ISerializer _serializer;
        private Dictionary<string, object>? _yamlData;

        public YamlCache(string? filePath)
        {
            _filePath = filePath;
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();
            _serializer = new SerializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            LoadYamlFile();
        }

        private void LoadYamlFile()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var yamlContent = File.ReadAllText(_filePath);
                    _yamlData = _deserializer.Deserialize<Dictionary<string, object>>(yamlContent);
                }
                else
                {
                    _yamlData = new Dictionary<string, object>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading YAML file: {ex.Message}");
                _yamlData = new Dictionary<string, object>();
            }
        }

        public string ReadOrUpdateEntry(string key, string? newValue = null)
        {
            return ReadOrUpdateEntry<string>(key, newValue);
        }

        public T ReadOrUpdateEntry<T>(string key, T newValue = default(T))
        {
            try
            {
                var keys = key.Split('.');
                var currentNode = _yamlData as object;

                for (int i = 0; i < keys.Length - 1; i++)
                {
                    if (currentNode is Dictionary<string, object> currentDict)
                    {
                        if (currentDict.ContainsKey(keys[i]))
                        {
                            currentNode = currentDict[keys[i]];
                        }
                        else
                        {
                            if (EqualityComparer<T>.Default.Equals(newValue, default(T)))
                            {
                                return default(T);
                            }
                            var newDict = new Dictionary<string, object>();
                            currentDict[keys[i]] = newDict;
                            currentNode = newDict;
                        }
                    }
                    else
                    {
                        return default(T);
                    }
                }

                var finalKey = keys[^1];

                if (currentNode is Dictionary<string, object> finalDict)
                {
                    if (finalDict.ContainsKey(finalKey))
                    {
                        var currentValue = finalDict[finalKey];

                        if (!EqualityComparer<T>.Default.Equals(newValue, default(T)) && !newValue.Equals(currentValue))
                        {
                            finalDict[finalKey] = newValue;
                            SaveYamlFile();
                        }

                        return ConvertValue<T>(currentValue);
                    }

                    if (!EqualityComparer<T>.Default.Equals(newValue, default(T)))
                    {
                        finalDict[finalKey] = newValue;
                        SaveYamlFile();
                        return newValue;
                    }
                }
                return default(T);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReadOrUpdateEntry: {ex.Message}");
                return default(T);
            }
        }

        private T ConvertValue<T>(object value)
        {
            try
            {
                if (value is T variable)
                {
                    return variable;
                }
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return default(T);
            }
        }

        private void SaveYamlFile()
        {
            try
            {
                var yamlContent = _serializer.Serialize(_yamlData);
                File.WriteAllText(_filePath, yamlContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving YAML file: {ex.Message}");
            }
        }
    }

    public class YAMLData
    {
        public static YamlCache CLASSIC_Main { get; set; } = new YamlCache("CLASSIC Data/databases/CLASSIC Main.yaml");
        public static YamlCache CLASSIC_Fallout4 { get; set; } = new YamlCache("CLASSIC Data/databases/CLASSIC Fallout4.yaml");
        public static YamlCache CLASSIC_Settings { get; set; } = new YamlCache("CLASSIC Settings.yaml");
        public static YamlCache CLASSIC_Ignore { get; set; } = new YamlCache("CLASSIC Ignore.yaml");
        public static YamlCache CLASSIC_Fallout4_Local { get; set; } = new YamlCache("CLASSIC Data/CLASSIC Fallout4 Local.yaml");

        public static dynamic? SettingsCheck(string key)
        {
            if (!Path.Exists("CLASSIC Settings.yaml"))
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                CLASSIC_Settings.ReadOrUpdateEntry(CLASSIC_Main.ReadOrUpdateEntry("CLASSIC_Info.default_settings").ToString());
                
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
            string? value = CLASSIC_Settings.ReadOrUpdateEntry(key);
            switch (value)
            {
                case "True":
                case "true":
                    return true;
                case "False":
                case "false":
                    return false;
                case null:
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    if (!CLASSIC_Settings.ReadOrUpdateEntry(key).Contains("Path"))
                    {
                        return value;
                    }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                    Console.WriteLine($"Error: {key} does not have a value in CLASSIC Settings.yaml.");
                    return null;
                default:
                    return value;
            }
        }
    }

    public class UpdateChecker
    {
        private static readonly HttpClient httpClient = new HttpClient();

        public static async Task<bool> ClassicUpdateCheck()
        {
            LoggerConfig.LogDebug("INITIATED UPDATE CHECK");
            if (YAMLData.SettingsCheck("Update Check") == true)
            {
                string? classicLocal = YAMLData.CLASSIC_Main.ReadOrUpdateEntry("CLASSIC_Info.version");
                Console.WriteLine("❓ (Needs internet connection) CHECKING FOR NEW CLASSIC VERSIONS...");
                Console.WriteLine("   (You can disable this check in CLASSIC Settings.yaml) \n");

                try
                {
                    HttpResponseMessage response = await httpClient.GetAsync("https://api.github.com/repos/evildarkarchon/CLASSIC-Fallout4/releases/latest");
                    response.EnsureSuccessStatusCode();

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonDocument = JsonDocument.Parse(jsonResponse);
                    string? classicVerReceived = jsonDocument.RootElement.GetProperty("name").GetString();
                    
                    Console.WriteLine($"Your CLASSIC Version: {classicLocal}\nNewest CLASSIC Version: {classicVerReceived}\n");

                    if (classicVerReceived == classicLocal)
                    {
                        Console.WriteLine("✔️ You have the latest version of CLASSIC! \n");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine(YAMLData.CLASSIC_Main.ReadOrUpdateEntry($"CLASSIC_Interface.update_warning_{Globals.Game}"));
                    }
                }
                catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(YAMLData.CLASSIC_Main.ReadOrUpdateEntry($"CLASSIC_Interface.update_unable_{Globals.Game}"));
                }
            }
            else
            {
                Console.WriteLine("\n❌ NOTICE: UPDATE CHECK IS DISABLED IN CLASSIC Settings.yaml \n");
                Console.WriteLine("===============================================================================");
            }
            return false;
        }
    }
    public class FileGenerator
    {
        public static void ClassicGenerateFiles()
        {
            string ignoreFilePath = "CLASSIC Ignore.yaml";
            string localYamlPath = $"CLASSIC Data/CLASSIC {Globals.Game} Local.yaml";
            string fidModsFilePath = $"CLASSIC Data/databases/{Globals.Game} FID Mods.txt";

            try
            {
                // Generate CLASSIC Ignore.yaml if it does not exist
                if (!File.Exists(ignoreFilePath))
                {
                    string? defaultIgnoreFile = YAMLData.CLASSIC_Main.ReadOrUpdateEntry("CLASSIC_Info.default_ignorefile");
                    File.WriteAllText(ignoreFilePath, defaultIgnoreFile);
                    LoggerConfig.LogInfo($"Generated {ignoreFilePath}");
                }

                // Generate CLASSIC Local.yaml if it does not exist
                if (!File.Exists(localYamlPath))
                {
                    string? defaultLocalYaml = YAMLData.CLASSIC_Main.ReadOrUpdateEntry("CLASSIC_Info.default_localyaml");
                    File.WriteAllText(localYamlPath, defaultLocalYaml);
                    LoggerConfig.LogInfo($"Generated {localYamlPath}");
                }

                // Generate FID Mods.txt if it does not exist
                if (!File.Exists(fidModsFilePath))
                {
                    if (Globals.Game == "Fallout4") {
                        string? defaultFidFile = YAMLData.CLASSIC_Fallout4.ReadOrUpdateEntry("Default_FIDMods");
                        File.WriteAllText(fidModsFilePath, defaultFidFile);
                    LoggerConfig.LogInfo($"Generated {fidModsFilePath}");
                    } /*else if (Globals.Game == "SkyrimSE") {
                        defaultFidFile = YAMLData.CLASSIC_SkyrimSE.ReadOrUpdateEntry($"CLASSIC Data/databases/CLASSIC {Globals.Game}.yaml", "Default_FIDMods");
                    } else {
                        defaultFidFile = YAMLData.CLASSIC_Other.ReadOrUpdateEntry($"CLASSIC Data/databases/CLASSIC {Globals.Game}.yaml", "Default_FIDMods");
                    */
                }
            }
            catch (Exception ex)
            {
                LoggerConfig.LogError($"An error occurred while generating files: {ex.Message}");
                throw;
            }
        }
    }
    public static class FileUtilities
    {
        public static void RemoveReadOnly(string filePath)
        {
            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    LoggerConfig.LogError($"Error: '{filePath}' not found.");
                    return;
                }

                // Get file attributes
                FileAttributes attributes = File.GetAttributes(filePath);

                // Check if file is read-only
                if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                {
                    // Remove read-only attribute
                    attributes &= ~FileAttributes.ReadOnly;
                    File.SetAttributes(filePath, attributes);

                    LoggerConfig.LogDebug($"'{filePath}' is no longer read-only.");
                }
                else
                {
                    LoggerConfig.LogDebug($"'{filePath}' is not set to read-only.");
                }
            }
            catch (FileNotFoundException ex)
            {
                LoggerConfig.LogError($"Error: '{filePath}' not found. {ex.Message}");
            }
            catch (Exception ex)
            {
                LoggerConfig.LogError($"Error: {ex.Message}");
            }
        }
    }
    public static class LoggerConfig
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static void ConfigureLogging()
        {
            var config = new NLog.Config.LoggingConfiguration();

            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "CLASSIC_Journal.log",
                Layout = "${longdate} | ${level:uppercase=true} | ${message}"
            };

            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);

            LogManager.Configuration = config;
        }

        public static void LogInfo(string message)
        {
            Logger.Info(message);
        }

        public static void LogError(string message)
        {
            Logger.Error(message);
        }
        public static void LogDebug(string message)
        {
            Logger.Debug(message);
        }

        // Add other log methods as needed
    }
    public class Globals {
        public static string Game { get; set; } = "Fallout4";
        public static string Vr { get; set; } = "";
        static void VrCheck()
        {
            if (YAMLData.SettingsCheck("VR Mode") == true)
            {
                Vr = "vr";
            }
        }
    }
    public class DataExtractor
    {
        public static void ClassicDataExtract()
        {
            string zipPath = "CLASSIC Data/CLASSIC Data.zip";
            string fallbackZipPath = "CLASSIC Data.zip";
            string extractPath = "CLASSIC Data";
            string mainYamlPath = "CLASSIC Data/databases/CLASSIC Main.yaml";
            string mainTxtPath = $"CLASSIC Data/databases/{Globals.Game} FID Main.txt";
            string dbPath = $"CLASSIC Data/databases/{Globals.Game} FormIDs.db";

            try
            {
                // Check and extract main YAML if it does not exist
                if (!File.Exists(mainYamlPath))
                {
                    using (ZipArchive zip = ZipFile.OpenRead(File.Exists(zipPath) ? zipPath : fallbackZipPath))
                    {
                        zip.ExtractToDirectory(extractPath, true);
                    }
                }

                // Check and extract main text file if it does not exist and database does not exist
                if (File.Exists(mainTxtPath) && !File.Exists(dbPath))
                {
                    DatabaseHandler.CreateFormIdDb();
                }
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Error: Unable to find necessary zip archive. {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
        }
    }

        public class DatabaseHandler
    {
        public static void CreateFormIdDb()
        {
            string dbPath = $"CLASSIC Data/databases/{Globals.Game} FormIDs.db";
            string txtFilePath = $"CLASSIC Data/databases/{Globals.Game} FID Main.txt";

            try
            {
                using (SQLiteConnection conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();

                    // Create table if not exists
                    string createTableQuery = $@"
                        CREATE TABLE IF NOT EXISTS {Globals.Game} (
                            id INTEGER PRIMARY KEY AUTOINCREMENT,  
                            plugin TEXT, 
                            formid TEXT, 
                            entry TEXT
                        )";
                    using (SQLiteCommand cmd = new SQLiteCommand(createTableQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Create index if not exists
                    string createIndexQuery = $"CREATE INDEX IF NOT EXISTS {Globals.Game}_index ON {Globals.Game}(formid, plugin COLLATE NOCASE)";
                    using (SQLiteCommand cmd = new SQLiteCommand(createIndexQuery, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }

                    // Read lines from text file and insert into database
                    using (StreamReader reader = new StreamReader(txtFilePath))
                    {
                        while (!reader.EndOfStream)
                        {
                            string? line = reader.ReadLine();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                            if (line.Contains('|'))
                            {
                                string[] parts = line.Split('|');
                                if (parts.Length >= 3)
                                {
                                    string plugin = parts[0].Trim();
                                    string formid = parts[1].Trim();
                                    string entry = parts[2].Trim();

                                    string insertQuery = $"INSERT INTO {Globals.Game} (plugin, formid, entry) VALUES (@plugin, @formid, @entry)";
                                    using (SQLiteCommand insertCmd = new SQLiteCommand(insertQuery, conn))
                                    {
                                        insertCmd.Parameters.AddWithValue("@plugin", plugin);
                                        insertCmd.Parameters.AddWithValue("@formid", formid);
                                        insertCmd.Parameters.AddWithValue("@entry", entry);
                                        insertCmd.ExecuteNonQuery();
                                    }
                                }
                            }
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        }
                    }
                }
            }
            catch (Exception ex) // TODO: Add more specific exception handling
            {
                Console.WriteLine($"An error occurred while creating/updating the database: {ex.Message}");
                throw;
            }
        }
    }
}