using System.Globalization;

namespace VERA.Converters
{
    // Gibt Primary-Farbe zurück wenn true, sonst Transparent
    public class BoolToActiveColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true
                ? Color.FromArgb("#3566E5")
                : Colors.Transparent;

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
