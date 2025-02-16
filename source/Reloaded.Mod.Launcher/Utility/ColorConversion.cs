using Color = System.Windows.Media.Color;

namespace Reloaded.Mod.Launcher.Utility;

/// <summary>
/// RGB/HSL conversion.
/// Authors: UweKeim and hacklex
/// Source: https://gist.github.com/UweKeim/fb7f829b852c209557bc49c51ba14c8b
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IColor<out T> where T : IColor<T>
{
    /// <summary>
    /// Constructs <see cref="T"/> from the specified <see cref="Color"/>.
    /// </summary>
    /// <param name="color">
    /// The color to convert.
    /// </param>
    /// <returns>
    /// The <see cref="T"/> instance that represents the specified RGB color.
    /// </returns>
    static abstract T FromColor(Color color);
    /// <summary>
    /// Converts the current color to a <see cref="Color"/>.
    /// </summary>
    /// <returns>
    /// The <see cref="Color"/> instance that represents the current color.
    /// </returns>
    Color ToColor();
    /// <summary>
    /// Compares the current color with a <see cref="Color"/> for visual equality.
    /// Two colors of zero opacity are considered equal, even if their other components differ.
    /// The comparison is done by converting the current color to a <see cref="Color"/> instance.
    /// </summary>
    /// <param name="other">
    /// The color to compare with the current color.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified color is visually equal to the current color; otherwise, <c>false</c>.
    /// </returns>
    bool IsVisuallyEqualTo(Color other)
    {
        var thisColor = ToColor();
        if (thisColor.A == 0 && other.A == 0) return true;
        return thisColor == other;
    }
    /// <summary>
    /// Compares the current color with another color for visual equality.
    /// Two colors of zero opacity are considered equal, even if their other components differ.
    /// The comparison is done by converting both the current color and the other color to <see cref="Color"/> instances.
    /// </summary>
    /// <param name="other">
    /// The color to compare with the current color.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified color is visually equal to the current color; otherwise, <c>false</c>.
    /// </returns>
    bool IsVisuallyEqualTo<TOther>(IColor<TOther> other) where TOther : IColor<TOther>
        => IsVisuallyEqualTo(other.ToColor());
}

/// <summary>
/// Contains extension methods for the <see cref="double"/> type.
/// </summary>
public static class MathExtensions
{
    private const double IsZeroTolerance = 0.00001;

    /// <summary>
    /// Determines whether the specified value can be considered zero.
    /// If the tolerance is not specified, the default value of 0.00001 is used. 
    /// </summary>
    /// <param name="x">The value to check</param>
    /// <param name="tolerance">The strictly positive tolerance value to use</param>
    /// <returns>
    /// <c>true</c> if the specified value can be considered zero; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsZero(this double x, double? tolerance = null) => Math.Abs(x) < (tolerance ?? IsZeroTolerance);
    /// <summary>
    /// Shifts the value to the specified range.
    /// </summary>
    /// <param name="value">The value to shift.</param>
    /// <param name="min">The minimum value of the range.</param>
    /// <param name="max">The maximum value of the range.</param>
    public static double WrapAround(this double value, double min, double max)
    {
        var range = max - min;
        var result = (value - min) % range;
        return min + (result < 0 ? result + range : result);
    }
    /// <summary>
    /// Simultaneously calculates the minimum and maximum values from the specified sequence of numbers.
    /// </summary>
    /// <param name="values">
    /// The sequence of numbers to process.
    /// </param>
    /// <returns>
    /// A tuple containing the minimum and maximum values from the specified sequence.
    /// </returns>
    public static (double min, double max) GetMinMax(params double[] values)
    {
        var minValue = values[0];
        var maxValue = values[0];

        if (values.Length >= 2)
        {
            for (var i = 1; i < values.Length; i++)
            {
                var num = values[i];
                minValue = Math.Min(minValue, num);
                maxValue = Math.Max(maxValue, num);
            }
        }
        return (minValue, maxValue);
    }
}

/// <summary>
/// Provides extension methods for color space conversion.
/// </summary>
/// <remarks>
/// https://en.wikipedia.org/wiki/HSL_and_HSV
/// </remarks>
public static class ColorExtensions
{
    public static RgbColor ToRgbColor(this Color color) => new(color.R, color.G, color.B, color.A);
    public static HsbColor ToHsbColor(this Color color) => color.ToRgbColor().ToHsbColor();
    public static HslColor ToHslColor(this Color color) => color.ToRgbColor().ToHslColor();

    public static HslColor ToHslColor<T>(this IColor<T> source) where T : IColor<T> => source.ToColor().ToHslColor();
    public static HsbColor ToHsbColor<T>(this IColor<T> source) where T : IColor<T> => source.ToColor().ToHsbColor();
    public static RgbColor ToRgbColor<T>(this IColor<T> source) where T : IColor<T> => source.ToColor().ToRgbColor();
}

/// <summary>
/// Represents an element of the HSB color space.
/// https://en.wikipedia.org/wiki/HSL_and_HSV
/// </summary>
/// <param name="Hue"><see cref="double"/>-valued Hue, from 0 to 360</param>
/// <param name="Saturation"><see cref="double"/>-valued Saturation, from 0 to 100</param>
/// <param name="Brightness"><see cref="double"/>-valued Brightness, from 0 to 100</param>
/// <param name="Alpha"><see cref="byte"/>-valued Alpha</param>
public readonly record struct HsbColor(double Hue, double Saturation, double Brightness, byte Alpha) : IColor<HsbColor>
{
    public override string ToString() => $@"Hue: {Hue:0}; saturation: {Saturation:0}; brightness: {Brightness:0}.";

    /// <summary>
    /// Compares the current color with another color for equality.
    /// </summary>
    /// <param name="other"> The color to compare with the current color. </param>
    /// <returns>
    /// <c>true</c> if the specified color is equal to the current color; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Two colors are considered equal if their components are equal.
    /// Even if both colors have zero opacity, they will only be considered equal if all other components are also equal.
    /// </remarks>
    public bool Equals(HsbColor other) => (Hue - other.Hue).IsZero() &&
                                          (Saturation - other.Saturation).IsZero() &&
                                          (Brightness - other.Brightness).IsZero() &&
                                          Alpha == other.Alpha;


    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Alpha.GetHashCode();
            hashCode = hashCode * 397 ^ Hue.GetHashCode();
            hashCode = hashCode * 397 ^ Saturation.GetHashCode();
            hashCode = hashCode * 397 ^ Brightness.GetHashCode();
            return hashCode;
        }
    }

    public static HsbColor FromColor(Color color) => color.ToHsbColor();
    public Color ToColor()
    {
        double red, green, blue;

        double h = Hue, s = Saturation / 100, b = Brightness / 100;

        if (s.IsZero()) red = green = blue = b;
        else
        {
            // the color wheel has six sectors.
            // Without this WrapAround, the smooth transition between 360 and 0 will be broken
            var sectorPosition = h.WrapAround(0, 360) / 60;
            var sectorNumber = (int)Math.Floor(sectorPosition);
            var fractionalSector = sectorPosition - sectorNumber;

            var p = b * (1 - s);
            var q = b * (1 - s * fractionalSector);
            var t = b * (1 - s * (1 - fractionalSector));

            // We save vertical screen space by using tuple deconstruction.
            (red, green, blue) = sectorNumber switch
            {
                0 => (b, t, p),
                1 => (q, b, p),
                2 => (p, b, t),
                3 => (p, q, b),
                4 => (t, p, b),
                _ => (b, p, q)
            };
        }

        var nRed = byte.CreateSaturating(red * 255);
        var nGreen = byte.CreateSaturating(green * 255);
        var nBlue = byte.CreateSaturating(blue * 255);
        return Color.FromArgb(Alpha, nRed, nGreen, nBlue);
    }
}

/// <summary>
/// Represents an element of the HSL color space.
/// https://en.wikipedia.org/wiki/HSL_and_HSV
/// </summary>
public readonly record struct HslColor(double Hue, double Saturation, double Light, byte Alpha) : IColor<HslColor>
{
    public override string ToString() => Alpha < 255
        ? $@"hsla({Hue:0}, {Saturation:0}%, {Light:0}%, {Alpha / 255f})"
        : $@"hsl({Hue:0}, {Saturation:0}%, {Light:0}%)";

    /// <summary>
    /// Compares the current color with another color for equality.
    /// </summary>
    /// <param name="other"> The color to compare with the current color. </param>
    /// <returns>
    /// <c>true</c> if the specified color is equal to the current color; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Two colors are considered equal if their components are equal.
    /// Even if both colors have zero opacity, they will only be considered equal if all other components are also equal.
    /// </remarks>
    public bool Equals(HslColor other) => (Hue - other.Hue).IsZero() &&
                                          (Saturation - other.Saturation).IsZero() &&
                                          (Light - other.Light).IsZero() &&
                                          Alpha == other.Alpha;

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = Alpha.GetHashCode();
            hashCode = hashCode * 397 ^ Hue.GetHashCode();
            hashCode = hashCode * 397 ^ Saturation.GetHashCode();
            hashCode = hashCode * 397 ^ Light.GetHashCode();
            return hashCode;
        }
    }

    private static double Hue2Rgb(double v1, double v2, double vH)
    {
        vH = vH.WrapAround(0, 1);
        if (6.0 * vH < 1.0) return v1 + (v2 - v1) * 6.0 * vH;
        if (2.0 * vH < 1.0) return v2;
        if (3.0 * vH < 2.0) return v1 + (v2 - v1) * (2.0 / 3.0 - vH) * 6.0;
        return v1;
    }

    public static HslColor FromColor(Color color) => color.ToHslColor();
    public Color ToColor()
    {

        double red, green, blue;

        var h = Hue / 360.0;
        var s = Saturation / 100.0;
        var l = Light / 100.0;

        if (s.IsZero()) red = green = blue = l;
        else
        {
            var var2 = l < 0.5 ? l + l * s : l + s - l * s;
            var var1 = 2.0 * l - var2;

            red = Hue2Rgb(var1, var2, h + 1.0 / 3.0);
            green = Hue2Rgb(var1, var2, h);
            blue = Hue2Rgb(var1, var2, h - 1.0 / 3.0);
        }

        var nRed = byte.CreateSaturating(red * 255);
        var nGreen = byte.CreateSaturating(green * 255);
        var nBlue = byte.CreateSaturating(blue * 255);
        return Color.FromArgb(Alpha, nRed, nGreen, nBlue);
    }
}

/// <summary>
/// Represents an element of the RGB color space.
/// All channel values are from 0 to 255.
/// </summary>
public readonly record struct RgbColor(byte Red, byte Green, byte Blue, byte Alpha) : IColor<RgbColor>
{
    public override string ToString() => Alpha < 255
        ? $@"rgba({Red}, {Green}, {Blue}, {Alpha / 255d})"
        : $@"rgb({Red}, {Green}, {Blue})";

    /// <summary>
    /// Compares the current color with another color for equality.
    /// </summary>
    /// <param name="other">
    /// The color to compare with the current color.
    /// </param>
    /// <returns>
    /// <c>true</c> if the specified color is equal to the current color; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method compares the colors component-wise.
    /// Two colors with zero opacity are NOT considered equal unless all other components are also equal.
    /// </remarks>
    public bool Equals(RgbColor other) => Red == other.Red &&
                                          Green == other.Green &&
                                          Blue == other.Blue &&
                                          Alpha == other.Alpha;
    public HsbColor ToHsbColor()
    {
        // We use double to increase the precision.
        var r = Red / 255d;
        var g = Green / 255d;
        var b = Blue / 255d;

        var (minValue, maxValue) = MathExtensions.GetMinMax(r, g, b);

        var delta = maxValue - minValue;

        double hue = 0;
        double saturation;
        var brightness = maxValue * 100;

        if (maxValue.IsZero() || delta.IsZero())
        {
            hue = 0;
            saturation = 0;
        }
        else
        {
            // The initial version had a mistake here. minValue was compared to zero,
            // instead of maxValue. Also, according to the wiki, the correct fallback is 0, not 100.
            saturation = maxValue.IsZero() ? 0 : delta / maxValue * 100;

            if ((r - maxValue).IsZero()) hue = (g - b) / delta;
            else if ((g - maxValue).IsZero()) hue = 2 + (b - r) / delta;
            else if ((b - maxValue).IsZero()) hue = 4 + (r - g) / delta;
        }

        hue = (hue < 0 ? hue + 6 : hue) * 60;

        return new(hue, saturation, brightness, Alpha);
    }
    public HslColor ToHslColor()
    {
        var varR = Red / 255.0; //input values are from 0 to 255 inclusive
        var varG = Green / 255.0;
        var varB = Blue / 255.0;

        var (minChannel, maxChannel) = MathExtensions.GetMinMax(varR, varG, varB);
        var maxDelta = maxChannel - minChannel; //Delta RGB value

        double h, l = (maxChannel + minChannel) / 2;

        if (maxDelta.IsZero()) return new HslColor(0, 0, l * 100.0, Alpha);
        var denominator = l < 0.5 ? maxChannel + minChannel : 2.0 - maxChannel - minChannel;
        double s = maxDelta / denominator;

        var delR = ((maxChannel - varR) / 6.0 + maxDelta / 2.0) / maxDelta;
        var delG = ((maxChannel - varG) / 6.0 + maxDelta / 2.0) / maxDelta;
        var delB = ((maxChannel - varB) / 6.0 + maxDelta / 2.0) / maxDelta;

        if ((varR - maxChannel).IsZero()) h = delB - delG;
        else if ((varG - maxChannel).IsZero()) h = 1.0 / 3.0 + delR - delB;
        else if ((varB - maxChannel).IsZero()) h = 2.0 / 3.0 + delG - delR;
        // ReSharper disable once CommentTypo
        else h = 0.0; // Uwe Keim

        h = h.WrapAround(0, 1);

        return new HslColor(h * 360.0, s * 100.0, l * 100.0, Alpha);
    }

    // When the entire structure fits into a single integer, we can use it as its own hash code.
    public override int GetHashCode() => Alpha << 24 | Blue << 16 | Green << 8 | Red;

    public static RgbColor FromColor(Color color) => color.ToRgbColor();
    public Color ToColor() => Color.FromArgb(Alpha, Red, Green, Blue);
}