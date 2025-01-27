﻿using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CLASSIC.Core.Configuration;

public class YamlSettingsCache
{
    private static readonly Lazy<YamlSettingsCache> LazyInstance = 
        new(() => new YamlSettingsCache());

    public static YamlSettingsCache Instance => LazyInstance.Value;

    private readonly Dictionary<string, Dictionary<string, object>> _cache = new();
    private readonly Dictionary<string, DateTime> _fileModTimes = new();
    private readonly IDeserializer _yamlDeserializer;
    private readonly ISerializer _yamlSerializer;

    private YamlSettingsCache()
    {
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();

        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
    }

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

    public T GetSetting<T>(YamlType yamlStore, string keyPath, T newValue = default)
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
            current[finalKey] = newValue;

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

        return default;
    }

    private string GetYamlPath(YamlType yamlType)
    {
        var basePath = AppContext.BaseDirectory;
            
        return yamlType switch
        {
            YamlType.Main => Path.Combine(basePath, "CLASSIC Data", "databases", "CLASSIC Main.yaml"),
            YamlType.Settings => Path.Combine(basePath, "CLASSIC Settings.yaml"),
            YamlType.Ignore => Path.Combine(basePath, "CLASSIC Ignore.yaml"), 
            YamlType.Game => Path.Combine(basePath, "CLASSIC Data", "databases", $"CLASSIC {ClassicApplication.Current.GameVars.Game}.yaml"),
            YamlType.GameLocal => Path.Combine(basePath, "CLASSIC Data", $"CLASSIC {ClassicApplication.Current.GameVars.Game} Local.yaml"),
            YamlType.Test => Path.Combine(basePath, "tests", "test_settings.yaml"),
            _ => throw new ArgumentException("Invalid YAML store type", nameof(yamlType))
        };
    }
}