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
}
