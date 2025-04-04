// See https://aka.ms/new-console-template for more information
using Reloaded.Mod.Loader.IO.Config;
using Reloaded.Mod.Loader.IO.Structs;
using RemixToolkit.HostMod.Installer;
using System.Diagnostics;

var reloadedModsDir = Environment.GetEnvironmentVariable("RELOADEDIIMODS", EnvironmentVariableTarget.User);
if (reloadedModsDir == null)
{
    Console.WriteLine("Failed to find Reloaded II mods folder.");
    return;
}

var mods = Directory.EnumerateDirectories(reloadedModsDir)
    .Select(x => Path.Join(x, ModConfig.ConfigFileName))
    .Where(File.Exists)
    .Select(x => new PathTuple<ModConfig>(x, IConfig<ModConfig>.FromPath(x)))
    .OrderBy(x => x.Config.ModName)
    .ToArray();

for (int i = 0; i < mods.Length; i++)
{
    Console.WriteLine($"[ {i + 1} ] {mods[i].Config.ModName}");
}

int id;

do
{
    Console.Write("\nAdd/Remove ReMIX Toolkit from Mod: ");
}
while (int.TryParse(Console.ReadLine(), out id) == false || id < 0 || id > mods.Length);

id--;
var selectedMod = mods[id];
var modDir = Path.GetDirectoryName(selectedMod.Path)!;
if (selectedMod.Config.ModDependencies.Contains(HostModInstaller.REMIX_MOD_ID))
{
    Console.WriteLine($"{selectedMod.Config.ModName}: Removing ReMIX Toolkit...");
    HostModInstaller.Uninstall(selectedMod);
    Console.WriteLine($"{selectedMod.Config.ModName}: ReMIX Toolkit Removed!");
}
else
{
    Console.WriteLine($"{selectedMod.Config.ModName}: Installing ReMIX Toolkit...");
    HostModInstaller.Install(selectedMod);
    Console.WriteLine($"{selectedMod.Config.ModName}: ReMIX Toolkit Installed!");
}

if (!Debugger.IsAttached) Console.ReadLine();