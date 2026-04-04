using System.Globalization;

namespace VERA.Converters
{
    // Gibt 0.4 zurück wenn true (gesperrt), sonst 1.0
    public class BoolToOpacityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is true ? 0.45 : 1.0;

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
