using Reloaded.Mod.Loader.IO.Remix.Configs.Models;

namespace Reloaded.Mod.Loader.IO.Remix.Configs;

public class DynamicConfigurator : IConfiguratorV1
{
    private readonly string _schemaFile;
    private readonly string _configFile;
    private readonly DynamicConfig _config;

    private string _modDir;

    public DynamicConfigurator(string schemaFile, string configFile)
    {
        _schemaFile = schemaFile;
        _configFile = configFile;
        _config = new DynamicConfig(_schemaFile, _configFile);
    }

    public IConfigurable[] GetConfigurations() => [_config];

    public DynamicConfig GetConfig() => _config;

    public void SetModDirectory(string modDirectory) => _modDir = modDirectory;

    public bool TryRunCustomConfiguration() => false;

    /// <summary>
    /// Creates configurator with config for a mod.
    /// </summary>
    /// <param name="modDir">Mod directory.</param>
    /// <param name="configDir">Config directory.</param>
    /// <returns></returns>
    public static DynamicConfigurator Create(string modDir, string configDir)
        => new DynamicConfigurator(GetModSchemaFile(modDir), Path.Join(configDir, "config.yaml"));

    public static string GetModSchemaFile(string modDir) => Path.Join(modDir, "remix", "config", DynamicConfigSchema.SchemaFileName);
}
