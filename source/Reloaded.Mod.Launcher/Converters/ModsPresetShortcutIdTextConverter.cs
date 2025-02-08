
using Reloaded.Mod.Loader.IO.Remix.Mods;

namespace Reloaded.Mod.Launcher.Converters;

public class ModsPresetShortcutIdTextConverter : IValueConverter
{
    public static readonly ModsPresetShortcutIdTextConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ModsPreset preset && parameter is ListView list)
        {
            var config = Lib.IoC.Get<LoaderConfig>();

            var index = list.Items.IndexOf(preset);
            if (index < 10 && index != 0 && config.PresetShortcutsEnabled)
            {
                return $"[ {index} ] ";
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
