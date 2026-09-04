using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace GenericParsingEngine.Config;

/// <summary>
/// Loads a <see cref="ParserConfig"/> from a YAML (.yaml / .yml) or JSON (.json) file.
/// </summary>
public class ConfigLoader
{
    /// <summary>
    /// Load and deserialize a config file.
    /// </summary>
    /// <param name="configPath">Absolute or relative path to the config file.</param>
    /// <returns>A populated <see cref="ParserConfig"/>.</returns>
    /// <exception cref="FileNotFoundException">If the file does not exist.</exception>
    /// <exception cref="NotSupportedException">If the file extension is not .yaml, .yml, or .json.</exception>
    public ParserConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"Config file not found: {configPath}");

        var ext = Path.GetExtension(configPath).ToLowerInvariant();
        var content = File.ReadAllText(configPath);

        return ext switch
        {
            ".yaml" or ".yml" => LoadYaml(content),
            ".json"           => LoadJson(content),
            _ => throw new NotSupportedException(
                $"Config format '{ext}' is not supported. Use .yaml, .yml, or .json")
        };
    }

    private static ParserConfig LoadYaml(string content)
    {
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        return deserializer.Deserialize<ParserConfig>(content)
               ?? throw new InvalidOperationException("Failed to deserialize YAML config: null result.");
    }

    private static ParserConfig LoadJson(string content)
    {
        return JsonSerializer.Deserialize<ParserConfig>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize JSON config: null result.");
    }
}
