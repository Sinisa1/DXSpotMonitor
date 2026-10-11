using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DXSpotMonitor.Utilities {

    public class BoolToColorConverter:IValueConverter {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
            if(value is bool boolValue) {
                return boolValue ? Colors.Green : Colors.Red;
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    public class BoolToStatusConverter:IValueConverter {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
            if(value is bool boolValue) {
                return boolValue ? "✓" : "✗";
            }
            return "?";
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    public class BoolToMonitoringConverter:IValueConverter {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
            if(value is bool boolValue) {
                return boolValue ? "⏸ MONITORING" : "⏹ STOPPED";
            }
            return "UNKNOWN";
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
    public class InverseBoolConverter:IValueConverter {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
            if(value is bool boolValue) {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
            if(value is bool boolValue) {
                return !boolValue;
            }
            return false;
        }
    }
    /// <summary>
    /// Highlights empty callsigns with yellow background
    /// </summary>
    public class EmptyStringToBackgroundConverter:IValueConverter {
        public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
            if(value is string str && string.IsNullOrWhiteSpace(str)) {
                return new SolidColorBrush(Colors.LightYellow);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}