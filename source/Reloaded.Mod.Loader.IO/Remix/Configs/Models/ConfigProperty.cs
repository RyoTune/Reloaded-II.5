namespace Reloaded.Mod.Loader.IO.Remix.Configs.Models;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ConfigProperty
{
    private const string DEFAULT_TYPE = "bool";

    public string Id { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Name { get; set; }

    public string Description { get; set; }

    public string Default { get; set; }

    public string Type { get; set; } = DEFAULT_TYPE;

    public Type GetPropertyType()
        => Type.ToLower() switch
        {
            "bool" or "toggle" => typeof(bool),
            "string" or "text" => typeof(string),
            "int" or "number" => typeof(int),
            "double" => typeof(double),
            _ => throw new NotImplementedException($"Unknown type: {Type}"),
        };

    public object GetDefaultValue()
    {
        if (Default == null) return null;
        return Convert.ChangeType(Default, GetPropertyType());
    }
}
