using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CLASSIC.Core.Configuration;

/// <summary>
/// A singleton class responsible for caching and manipulating settings stored in YAML files.
/// </summary>
/// <remarks>
/// This class provides functionalities to load, cache, retrieve, and update YAML settings.
/// It supports efficient access to YAML data by caching loaded files and updating their data
/// directly when changes are made.
/// </remarks>
public class YamlSettingsCache
{
    private static readonly Lazy<YamlSettingsCache> LazyInstance = 
        new(() => new YamlSettingsCache());

    public static YamlSettingsCache Instance => LazyInstance.Value;

    private readonly Dictionary<string, Dictionary<string, object>> _cache = new();
    private readonly Dictionary<string, DateTime> _fileModTimes = new();
    private readonly IDeserializer _yamlDeserializer;
    private readonly ISerializer _yamlSerializer;

    /// <summary>
    /// A singleton class that handles caching, retrieval, and updating of settings stored in YAML files.
    /// </summary>
    /// <remarks>
    /// This class ensures efficient access to YAML configuration files by maintaining a cache.
    /// It supports loading YAML files, updating their content, and retrieving specific settings
    /// with optional support for updating values.
    /// </remarks>
    private YamlSettingsCache()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

    /// <summary>
    /// Loads and parses a YAML file into a dictionary structure, caching it for efficient future access.
    /// </summary>
    /// <param name="yamlPath">The file path to the YAML file to be loaded.</param>
    /// <returns>A dictionary containing the parsed YAML data. If the file does not exist, an empty dictionary is returned.</returns>
    private Dictionary<string, object> LoadYaml(string yamlPath)
    {
        var path = new FileInfo(yamlPath);
        if (!path.Exists) return new Dictionary<string, object>();

        var lastModTime = path.LastWriteTime;

        if (_cache.TryGetValue(yamlPath, out var yaml))
        {
            if (_fileModTimes[yamlPath] == lastModTime)
            {
                return yaml;
            }
        }

        // Update file modification time
        _fileModTimes[yamlPath] = lastModTime;

        // Reload YAML file
        using var reader = new StreamReader(yamlPath);
        var yamlText = reader.ReadToEnd();
        var yamlData = _yamlDeserializer.Deserialize<Dictionary<string, object>>(yamlText);
        _cache[yamlPath] = yamlData;

        return yamlData;
    }

    /// <summary>
    /// Retrieves or updates a setting specified by a key path within a YAML file.
    /// </summary>
    /// <typeparam name="T">The type of the setting value to retrieve or update.</typeparam>
    /// <param name="yamlStore">The YAML type that specifies which YAML file to access.</param>
    /// <param name="keyPath">The hierarchical path to the setting in the YAML structure.</param>
    /// <param name="newValue">
    /// The new value to assign to the setting. If not provided or is the default,
    /// the method retrieves the existing value instead of updating it.
    /// </param>
    /// <returns>
    /// The value of the setting at the specified key path. If a new value is provided, it returns that value.
    /// If the key is not found or no value is assigned, it returns the default value of the type.
    /// </returns>
#pragma warning disable CS8601 // Possible null reference assignment.
    public T GetSetting<T>(YamlType yamlStore, string keyPath, T newValue = default)
#pragma warning restore CS8601 // Possible null reference assignment.
    {
        var yamlPath = GetYamlPath(yamlStore);
        var data = LoadYaml(yamlPath);
        var keys = keyPath.Split('.');

        // Navigate through the YAML structure
        var current = data;
        for (var i = 0; i < keys.Length - 1; i++)
        {
            var key = keys[i];
            if (!current.ContainsKey(key))
            {
                current[key] = new Dictionary<string, object>();
            }
            current = (Dictionary<string, object>)current[key];
        }

        var finalKey = keys[^1];

        // If newValue provided, update the value
        if (!EqualityComparer<T>.Default.Equals(newValue, default))
        {
#pragma warning disable CS8601 // Possible null reference assignment.
            current[finalKey] = newValue;
#pragma warning restore CS8601 // Possible null reference assignment.

            // Write changes back to YAML file
            var yaml = _yamlSerializer.Serialize(data);
            File.WriteAllText(yamlPath, yaml);

            // Update cache
            _cache[yamlPath] = data;
            return newValue;
        }

        // Get existing value
        if (current.TryGetValue(finalKey, out var value))
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }

#pragma warning disable CS8603 // Possible null reference return.
        return default;
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    /// Resolves the file path for a given YAML file type based on the specified <see cref="YamlType"/>.
    /// </summary>
    /// <param name="yamlType">The type of the YAML file to retrieve the path for, as defined by the <see cref="YamlType"/> enumeration.</param>
    /// <returns>Returns the absolute file path for the specified YAML type.</returns>
    /// <exception cref="ArgumentException">Thrown when an invalid <see cref="YamlType"/> is provided.</exception>
    private string GetYamlPath(YamlType yamlType)
    {
        var basePath = AppContext.BaseDirectory;
            
        return yamlType switch
        {
            YamlType.Main => Path.Combine(basePath, "CLASSIC Data", "databases", "CLASSIC Main.yaml"),
            YamlType.Settings => Path.Combine(basePath, "CLASSIC Settings.yaml"),
            YamlType.Ignore => Path.Combine(basePath, "CLASSIC Ignore.yaml"), 
            YamlType.Game => Path.Combine(basePath, "CLASSIC Data", "databases", $"CLASSIC {ClassicApplication.Current!.GameVars.Game}.yaml"),
            YamlType.GameLocal => Path.Combine(basePath, "CLASSIC Data", $"CLASSIC {ClassicApplication.Current!.GameVars.Game} Local.yaml"),
            YamlType.Test => Path.Combine(basePath, "tests", "test_settings.yaml"),
            _ => throw new ArgumentException("Invalid YAML store type", nameof(yamlType))
        };
    }
}