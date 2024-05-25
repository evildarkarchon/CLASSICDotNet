using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CLASSICDotNet
{
    public class YamlCache
    {
        private readonly string _filePath;
        private readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        private readonly IDeserializer _deserializer;
        private readonly ISerializer _serializer;
        private Dictionary<string, object> _yamlData;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public YamlCache(string filePath)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            _filePath = filePath;
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();
            _serializer = new SerializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
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
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access to the file is denied: {ex.Message}");
                _yamlData = new Dictionary<string, object>();
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O error while reading the file: {ex.Message}");
                _yamlData = new Dictionary<string, object>();
            }
            catch (YamlException ex)
            {
                Console.WriteLine($"Error deserializing YAML content: {ex.Message}");
                _yamlData = new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                _yamlData = new Dictionary<string, object>();
            }
        }

        public string? ReadOrUpdateEntry(string key, string? newValue = null)
        {
            try
            {
                if (_cache.ContainsKey(key))
                {
                    return _cache[key];
                }

                var keys = key.Split('.');
                var currentNode = _yamlData;

                for (int i = 0; i < keys.Length - 1; i++)
                {
                    if (currentNode.ContainsKey(keys[i]) && currentNode[keys[i]] is Dictionary<string, object> nextNode)
                    {
                        currentNode = nextNode;
                    }
                    else
                    {
                        if (newValue == null)
                        {
                            return null;
                        }
                        currentNode[keys[i]] = new Dictionary<string, object>();
                        currentNode = (Dictionary<string, object>)currentNode[keys[i]];
                    }
                }

                var finalKey = keys[^1];

                if (currentNode.ContainsKey(finalKey))
                {
                    var currentValue = currentNode[finalKey].ToString();
#pragma warning disable CS8601 // Possible null reference assignment.
                    _cache[key] = currentValue;
#pragma warning restore CS8601 // Possible null reference assignment.

                    if (newValue != null && newValue != currentValue)
                    {
                        currentNode[finalKey] = newValue;
                        _cache[key] = newValue;
                        SaveYamlFile();
                    }

                    return currentValue;
                }

                if (newValue != null)
                {
                    currentNode[finalKey] = newValue;
                    _cache[key] = newValue;
                    SaveYamlFile();
                    return newValue;
                }

                return null;
            }
            catch (KeyNotFoundException ex)
            {
                Console.WriteLine($"Key not found: {ex.Message}");
                return null;
            }
            catch (InvalidCastException ex)
            {
                Console.WriteLine($"Invalid cast operation: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
                return null;
            }
        }

        private void SaveYamlFile()
        {
            try
            {
                var yamlContent = _serializer.Serialize(_yamlData);
                File.WriteAllText(_filePath, yamlContent);
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Access to the file is denied: {ex.Message}");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"I/O error while writing the file: {ex.Message}");
            }
            catch (YamlException ex)
            {
                Console.WriteLine($"Error serializing YAML content: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error: {ex.Message}");
            }
        }
    }
}