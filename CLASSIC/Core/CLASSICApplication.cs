using System;
using System.IO;
using CLASSIC.Core.Configuration;
using CLASSIC.Core.IO;
using CLASSIC.Core.Logging;

namespace CLASSIC.Core;

public class ClassicApplication
{
    private static ClassicApplication _current;
    public static ClassicApplication Current 
    {
        get
        {
            if (_current == null)
                throw new InvalidOperationException("ClassicApplication has not been initialized. Call Initialize() first.");
            return _current;
        }
        private set => _current = value;
    }

    public GameVars GameVars { get; }
    public bool IsGui { get; }

    private ClassicApplication(bool isGui)
    {
        IsGui = isGui;
        GameVars = new GameVars
        {
            VR = GetClassicSetting<bool>("VR Mode") ? "VR" : ""
        };

        Logger.Configure();
    }

    public static void Initialize(bool isGui = false)
    {
        if (_current != null)
            throw new InvalidOperationException("ClassicApplication has already been initialized.");
                
        Current = new ClassicApplication(isGui);
    }

    public T GetClassicSetting<T>(string setting)
    {
        var settingsPath = new FileInfo(PathUtilities.GetRelativePath("CLASSIC Settings.yaml"));
        if (!settingsPath.Exists)
        {
            var defaultSettings = YamlSettingsCache.Instance.GetSetting<string>(YamlType.Main, "CLASSIC_Info.default_settings");
            if (string.IsNullOrEmpty(defaultSettings))
            {
                throw new InvalidOperationException("Invalid Default Settings in 'CLASSIC Main.yaml'");
            }

            File.WriteAllText(settingsPath.FullName, defaultSettings);
        }

        return YamlSettingsCache.Instance.GetSetting<T>(YamlType.Settings, $"CLASSIC_Settings.{setting}");
    }

    public void SetClassicSetting<T>(string setting, T value)
    {
        YamlSettingsCache.Instance.GetSetting(YamlType.Settings, $"CLASSIC_Settings.{setting}", value);
    }
}