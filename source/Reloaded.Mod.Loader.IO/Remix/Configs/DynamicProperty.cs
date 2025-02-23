namespace Reloaded.Mod.Loader.IO.Remix.Configs;

public class DynamicProperty : PropertyDescriptor
{
    private readonly Type _type;
    private readonly object _initialValue;
    private object _value;

    public DynamicProperty(string name, Type type, Attribute[] attrs, object initialValue = null)
        : base(name, attrs)
    {
        _type = type;
        _value = initialValue;
        _initialValue = initialValue;
    }

    public DynamicProperty(MemberDescriptor descr) : base(descr)
    {
    }

    public DynamicProperty(MemberDescriptor descr, Attribute[] attrs) : base(descr, attrs)
    {
    }

    public DynamicProperty(string name, Attribute[] attrs) : base(name, attrs)
    {
    }

    public override Type ComponentType => typeof(DynamicConfig);

    public override bool IsReadOnly { get; } = false;

    public override Type PropertyType => _type!;

    public override bool CanResetValue(object component) => true;

    public override object GetValue(object component) => _value;

    public override void ResetValue(object component) => _value = _initialValue;

    public override void SetValue(object component, object value) => _value = Convert.ChangeType(value, PropertyType);

    public override bool ShouldSerializeValue(object component) => true;
}