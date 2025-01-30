namespace Reloaded.Mod.Launcher.Lib.Models.Model.Application;

/// <summary>
/// Dummy app config so unknown applications from mods
/// can be used where needed, such as in a mod's supported apps.
/// </summary>
/// <param name="appId"></param>
public class UnknownApplicationConfig(string appId) : IApplicationConfig
{
    /// <summary/>
    public string AppId { get; set; } = appId;

    /// <summary/>
    public string AppName { get; set; } = string.Empty;

    /// <summary/>
    public Dictionary<string, object> PluginData { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary/>
    public string AppLocation { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary/>
    public string AppArguments { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary/>
    public string AppIcon { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    /// <summary/>
    public string[] EnabledMods { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
}
