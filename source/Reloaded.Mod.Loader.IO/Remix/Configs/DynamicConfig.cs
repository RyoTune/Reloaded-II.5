using Reloaded.Mod.Loader.IO.Remix.Configs.Models;
using Reloaded.Mod.Loader.IO.Remix.Serializers;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;

namespace Reloaded.Mod.Loader.IO.Remix.Configs;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public class DynamicConfig : DynamicObject, IConfigurable
{
    private readonly DynamicConfigSchema _schema;
    private readonly Dictionary<string, ConfigSetting> _settings;
    private readonly Dictionary<string, DynamicProperty> _properties;

    /// <summary>
    /// Dynamic config.
    /// </summary>
    /// <param name="schemaFile">Schema of the config, such as settings and actions.</param>
    /// <param name="configFile">Config file to save and load settings from.</param>
    public DynamicConfig(string schemaFile, string configFile)
    {
        _schema = YamlSerializer.DeserializeFile<DynamicConfigSchema>(schemaFile);
        _settings = _schema.Settings.ToDictionary(x => x.Id);
        _properties = _schema.Settings.Select(x => new DynamicProperty(x.Id, x.GetPropertyType(), ResolveAttributes(x), x.GetDefaultValue())).ToDictionary(x => x.Name);
        PropertyDescriptors = _properties.Values.ToArray();

        // Load current settings.
        if (File.Exists(configFile))
        {
            var config = YamlSerializer.DeserializeFile<Dictionary<string, object>>(configFile);
            foreach (var prop in PropertyDescriptors)
            {
                if (config.TryGetValue(prop.Name, out var value))
                {
                    prop.SetValue(this, value);
                }
            }
        }

        Save = () => YamlSerializer.SerializeFile(configFile, _properties.ToDictionary(x => x.Key, x => x.Value.GetValue(this)));
    }

    public PropertyDescriptor[] PropertyDescriptors { get; }

    public string ConfigName { get; } = "Default Config";

    public Action Save { get; }

    public object GetSettingValue(string name)
    {
        if (_settings[name].ValueOn != null || _settings[name].ValueOff != null)
        {
            var settingBool = (bool)_properties[name].GetValue(this);
            if (settingBool)
            {
                return _settings[name].ValueOn;
            }
            else
            {
                return _settings[name].ValueOff;
            }
        }

        return _properties[name].GetValue(this);
    }

    public ConfigAction[] Actions => _schema.Actions;

    public Dictionary<string, string> Constants => _schema.Constants;

    public Dictionary<string, object[]> Lists
        => _schema.Settings.Where(x => !string.IsNullOrEmpty(x.List)).GroupBy(x => x.List).ToDictionary(x => x.Key, x => x.Select(setting => GetSettingValue(setting.Id)).Where(x => x != null).ToArray());

    public override IEnumerable<string> GetDynamicMemberNames() => _properties.Keys;

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
        if (_properties.TryGetValue(binder.Name, out var prop))
        {
            result = prop.GetValue(this);

            // No auto conversion to final double in control property
            // with DynamicObject...
            if (prop.PropertyType == typeof(int))
            {
                result = Convert.ToDouble(result);
            }

            return true;
        }

        return base.TryGetMember(binder, out result);
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
        if (_properties.TryGetValue(binder.Name, out var prop))
        {
            prop.SetValue(this, value);
            return true;
        }

        return base.TrySetMember(binder, value);
    }

    private static Attribute[] ResolveAttributes(ConfigSetting property)
    {
        var attributes = new List<Attribute>()
        {
            new DisplayAttribute() { Order = -1 },
            new DefaultValueAttribute(property.GetDefaultValue()), // Config reset won't work without a default value attribute, surprisingly.
        };

        if (!string.IsNullOrEmpty(property.Name))
        {
            attributes.Add(new DisplayNameAttribute(property.Name));
        }

        if (!string.IsNullOrEmpty(property.Description))
        {
            attributes.Add(new DescriptionAttribute(property.Description));
        }

        if (!string.IsNullOrEmpty(property.Category))
        {
            attributes.Add(new CategoryAttribute(property.Category));
        }

        return attributes.ToArray();
    }
}
