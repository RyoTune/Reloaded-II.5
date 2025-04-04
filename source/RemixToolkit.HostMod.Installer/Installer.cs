using Reloaded.Mod.Loader.IO.Config;
using Reloaded.Mod.Loader.Update.Providers;
using Reloaded.Mod.Loader.Update.Providers.GitHub;
using static Reloaded.Mod.Loader.Update.Providers.GitHub.GitHubReleasesUpdateResolverFactory;
using Reloaded.Mod.Interfaces.Utilities;
using SevenZip;

namespace RemixToolkit.HostMod.Installer;

public static class Installer
{
    public const string REMIX_MOD_ID = "RemixToolkit.Reloaded";
    private const string REMIX_MOD_DLL = "RemixToolkit.HostMod.dll";
    private static readonly string[] REMIX_MOD_FILES =
    [
        REMIX_MOD_DLL,
        "YamlDotNet.dll",
        "RemixToolkit.Core.Configs.dll",
        "RemixToolkit.Core.Serializers.dll",
        "RemixToolkit.Interfaces.dll",
        "RemixToolkit.HostMod.deps.json",
    ];

    private static readonly DependencyResolverItem<GitHubConfig> REMIX_GITHUB_DEP = new()
    {
        Config = new()
        {
            RepositoryName = "RemixToolkit",
            UseReleaseTag = false,
            UserName = "RyoTune",
        },

        ReleaseMetadataName = "RemixToolkit.Reloaded.ReleaseMetadata.json",
    };

    public static void Install(string modDir)
    {
        var modConfigFile = Path.Join(modDir, ModConfig.ConfigFileName);
        if (!File.Exists(modConfigFile)) return;

        var mod = IConfig<ModConfig>.FromPathOrDefault(modConfigFile);

        if (!mod.ModDependencies.Contains(REMIX_MOD_ID))
        {
            mod.ModDependencies = [REMIX_MOD_ID, ..mod.ModDependencies];

            mod.PluginData.TryAdd(GitHubReleasesDependencyMetadataWriter.PluginId, new DependencyResolverMetadata<GitHubConfig>());
            mod.PluginData.TryGetValue<DependencyResolverMetadata<GitHubConfig>>(GitHubReleasesDependencyMetadataWriter.PluginId, out var githubDeps);

            if (!githubDeps.IdToConfigMap.ContainsKey(REMIX_MOD_ID))
            {
                githubDeps.IdToConfigMap[REMIX_MOD_ID] = REMIX_GITHUB_DEP;
            }
        }

        mod.ModDll = REMIX_MOD_DLL;
        var seven = new SevenZipExtractor(GetHostModPath());
        seven.ExtractFiles(modDir, REMIX_MOD_FILES);

        IConfig<ModConfig>.ToPath(mod, modConfigFile);
    }

    public static void Uninstall(string modDir)
    {
        var modConfigFile = Path.Join(modDir, ModConfig.ConfigFileName);
        if (!File.Exists(modConfigFile)) return;

        var mod = IConfig<ModConfig>.FromPathOrDefault(modConfigFile);
        if (mod.ModDependencies.Contains(REMIX_MOD_ID))
        {
            mod.ModDependencies = mod.ModDependencies.Where(x => x != REMIX_MOD_ID).ToArray();

            if (mod.PluginData.TryGetValue<DependencyResolverMetadata<GitHubConfig>>(GitHubReleasesDependencyMetadataWriter.PluginId, out var githubDeps))
            {
                githubDeps.IdToConfigMap.Remove(REMIX_MOD_ID);
            }
        }

        mod.ModDll = string.Empty;

        foreach (var fileName in REMIX_MOD_FILES)
        {
            File.Delete(Path.Join(modDir, fileName));
        }

        IConfig<ModConfig>.ToPath(mod, modConfigFile);
    }

    private static string GetHostModPath()
    {
        var hostModDir = Path.Join(Directory.GetCurrentDirectory(), "HostMod");
        return Directory.EnumerateFiles(hostModDir, "RemixToolkit.HostMod*.7z").First();
    }
}
