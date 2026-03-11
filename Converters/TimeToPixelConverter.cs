using System;
using System.Globalization;
using System.Windows.Data;

namespace VideoEditor.Converters
{
    public class TimeToPixelConverter : IMultiValueConverter
    {
        // How many pixels represent 1 second of time
        private const double PixelsPerSecond = 10.0;

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length > 0 && values[0] is TimeSpan time)
            {
                // Simple conversion: Seconds * Scale
                return time.TotalSeconds * PixelsPerSecond;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
