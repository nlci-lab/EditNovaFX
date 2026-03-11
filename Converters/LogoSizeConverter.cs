using System;
using System.Globalization;
using System.Windows.Data;

namespace VideoEditor.Converters
{
    public class LogoSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double scale && parameter != null && double.TryParse(parameter.ToString(), out double baseSize))
            {
                return baseSize * scale;
            }
            return 100.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
