using System;
using System.IO;
using CLASSIC.Core.Configuration;
using CLASSIC.Core.IO;
using CLASSIC.Core.Logging;

namespace CLASSIC.Core;

/// <summary>
/// The ClassicApplication class serves as the primary entry point for
/// CLASSIC framework-driven applications. It is responsible for handling
/// initialization, configuration, and runtime settings.
/// This class must be initialized before use by calling the Initialize method,
/// which sets up the application's context and loads relevant settings.
/// The application can operate in either GUI or non-GUI mode, as dictated
/// by the isGui parameter during initialization.
/// </summary>
public class ClassicApplication
{
    private static ClassicApplication? _current;

    /// <summary>
    /// Gets the current instance of the <see cref="ClassicApplication"/> class.
    /// This property returns the instance of the application that has been
    /// initialized using the <see cref="Initialize"/> method. If the application
    /// has not been initialized, accessing this property will throw an
    /// <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to access this property before the application
    /// has been initialized by calling <see cref="Initialize"/>.
    /// </exception>
    public static ClassicApplication? Current 
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
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public bool IsGui { get; }

    /// <summary>
    /// Represents the central application object for CLASSIC framework-driven
    /// applications, providing configuration, logging, and runtime settings management.
    /// Responsible for initializing application state and providing both GUI and non-GUI modes of operation.
    /// </summary>
    private ClassicApplication(bool isGui)
    {
        IsGui = isGui;
        GameVars = new GameVars
        {
            Vr = GetClassicSetting<bool>("VR Mode") ? "VR" : ""
        };

        Logger.Configure();
    }

    /// <summary>
    /// Initializes the ClassicApplication instance with the specified mode of operation.
    /// This method must be called before any other interaction with the
    /// ClassicApplication class. It sets up the application's initial context,
    /// configuration, and runtime settings. Only one initialization is allowed
    /// per application lifecycle.
    /// </summary>
    /// <param name="isGui">
    /// Specifies whether the application should operate in GUI mode.
    /// If set to true, the application will run in GUI mode; otherwise,
    /// it will run in non-GUI mode. The default value is false.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this method is called more than once during the application lifecycle.
    /// </exception>
    public static void Initialize(bool isGui = false)
    {
        if (_current != null)
            throw new InvalidOperationException("ClassicApplication has already been initialized.");
                
        Current = new ClassicApplication(isGui);
    }

    /// <summary>
    /// Retrieves a specific setting value of the specified type from the CLASSIC settings file.
    /// Automatically ensures the availability of the settings file,
    /// creating it with default values if it does not already exist.
    /// </summary>
    /// <typeparam name="T">The expected type of the setting value to retrieve.</typeparam>
    /// <param name="setting">The name of the setting to retrieve from the settings file.</param>
    /// <returns>The value of the specified setting as the type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the default settings in the 'CLASSIC Main.yaml' are invalid or unavailable.
    /// </exception>
    // ReSharper disable once MemberCanBePrivate.Global
    public T? GetClassicSetting<T>(string setting)
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

    /// <summary>
    /// Sets a specific configuration setting in the CLASSIC framework.
    /// This method updates the value of a given setting in the YAML-based settings cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to be associated with the specified setting.</typeparam>
    /// <param name="setting">The name of the setting to update.</param>
    /// <param name="value">The new value to be assigned to the specified setting.</param>
    // ReSharper disable once UnusedMember.Global
    public void SetClassicSetting<T>(string setting, T? value)
    {
        YamlSettingsCache.Instance.GetSetting(YamlType.Settings, $"CLASSIC_Settings.{setting}", value);
    }
}