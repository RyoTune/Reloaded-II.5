using ColorThiefDotNet;

namespace Reloaded.Mod.Launcher.Converters;

internal class ApplicationPathTupleToColorConverter : IValueConverter
{
    public static readonly ApplicationPathTupleToColorConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is PathTuple<ApplicationConfig> tuple)
        {
            return ApplicationIcon.GetColor(tuple);
        }

        return Colors.Pink;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
