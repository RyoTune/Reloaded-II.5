using CommunityToolkit.Mvvm.ComponentModel;
using Reloaded.Mod.Launcher.Lib.Remix.Updates;
using Reloaded.Mod.Loader.IO.Remix.Configs;
using ObservableObject = CommunityToolkit.Mvvm.ComponentModel.ObservableObject;

namespace Reloaded.Mod.Launcher.Lib.Remix.Mods;

public partial class ModStatus : ObservableObject
{
    private readonly static Type[] _sharedTypes = { typeof(IConfiguratorV1) };

    [ObservableProperty]
    private bool _isConfigurable;

    [ObservableProperty]
    private UpdateStatus _updates;

    private PathTuple<ModConfig> _tuple;
    private DateTime _dllLastWrite;

    /// <summary/>
    public ModStatus(PathTuple<ModConfig> tuple)
    {
        _tuple = tuple;

        Refresh();
    }

    /// <summary>
    /// Refreshes mod status.
    /// </summary>
    /// <param name="newTuple">Optionally, update the tuple this mod status is connected to.</param>
    public void Refresh(PathTuple<ModConfig>? newTuple = null)
    {
        if (newTuple != null)
        {
            _tuple = newTuple;
        }

        IsConfigurable = HasConfigurator(_tuple);
        Updates = PackageResolverFactory.HasAnyConfiguredResolver(_tuple) ? UpdateStatus.Supported : UpdateStatus.None;

        if (Updates == UpdateStatus.Supported)
        {
            if (UpdateService.IsChecking)
            {
                Updates = UpdateStatus.Checking;
            }
            else
            {
                Updates = UpdateService.HasModUpdate(_tuple) ? UpdateStatus.Pending : UpdateStatus.Supported;
            }
        }
    }

    // Disallowed inlining to ensure nothing from library can be kept alive by stack references etc.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool HasConfigurator(PathTuple<ModConfig> tuple)
    {
        var config = tuple.Config;
        var modDirectory = Path.GetFullPath(Path.GetDirectoryName(tuple.Path)!);

        if (File.Exists(DynamicConfigurator.GetModSchemaFile(modDirectory))) return true;

        string dllPath = config.GetManagedDllPath(tuple.Path);

        if (!File.Exists(dllPath))
            return false;

        // Re-use previous configurator check if DLL (likely) hasn't changed.
        // Also fixes performance issues from Reloaded refreshing mods constantly.
        var dllCurrentWrite = new FileInfo(dllPath).LastWriteTimeUtc;
        if (_dllLastWrite == dllCurrentWrite)
        {
            return IsConfigurable;
        }

        using var loader = PluginLoader.CreateFromAssemblyFile(dllPath, true, _sharedTypes, config =>
        {
            config.DefaultContext = AssemblyLoadContext.GetLoadContext(Assembly.GetExecutingAssembly())!;
            config.IsLazyLoaded = true;
            config.LoadInMemory = true;
        });

        var assembly = loader.LoadDefaultAssembly();
        var types = assembly.GetTypes();
        var entryPoint = types.FirstOrDefault(t => typeof(IConfiguratorV1).IsAssignableFrom(t) && !t.IsAbstract);

        _dllLastWrite = dllCurrentWrite;
        return entryPoint != null;
    }
}
