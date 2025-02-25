namespace Reloaded.Mod.Loader.IO.Remix.Configs.Models;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class ConfigSetting
{
    private const string DEFAULT_TYPE = "bool";

    /// <summary>
    /// ID used to get value in actions.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Property type of setting.
    /// </summary>
    public string Type { get; set; } = DEFAULT_TYPE;

    /// <summary>
    /// (Optional) Display name for property.
    /// </summary>
    public string Name { get; set; } = null;

    /// <summary>
    /// (Optional) Setting category.
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// (Optional) Setting description.
    /// </summary>
    public string Description { get; set; } = null;

    /// <summary>
    /// (Optional) Setting default value. If not set, the property type default will be used.
    /// </summary>
    public string Default { get; set; } = null;

    /// <summary>
    /// (Optional) List to add property to, for use in actions.
    /// </summary>
    public string List { get; set; } = null;

    /// <summary>
    /// (Optional) Actual value to use if this is a bool property and is enabled.
    /// </summary>
    public string ValueOn { get; set; } = null;

    /// <summary>
    /// (Optional) Actual value to use if this is a bool property and is disabled.
    /// </summary>
    public string ValueOff { get; set; } = null;

    public Type GetPropertyType()
        => Type.ToLower() switch
        {
            "bool" or "toggle" => typeof(bool),
            "string" or "text" => typeof(string),

            "int" or "number" => typeof(int),
            "byte" => typeof(byte),
            "short" => typeof(short),
            "float" => typeof(float),
            "double" => typeof(double),
            _ => throw new NotImplementedException($"Unknown type: {Type}"),
        };

    public object GetDefaultValue()
    {
        var type = GetPropertyType();

        // No default value set, use default value of prop type.
        if (Default == null)
        {
            if (type.IsValueType) return Activator.CreateInstance(type);

            return null;
        }

        return Convert.ChangeType(Default, type);
    }
}
