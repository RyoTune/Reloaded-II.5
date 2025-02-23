namespace Reloaded.Mod.Loader.IO.Remix.Configs.Models;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class DynamicConfigSchema
{
    public const string SchemaFileName = "schema.yaml";
    
    public Dictionary<string, string> Constants { get; set; } = [];

    public ConfigProperty[] Settings { get; set; } = [];

    public ConfigAction[] Actions { get; set; } = [];
}
