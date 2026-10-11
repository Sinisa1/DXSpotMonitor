using System.Globalization;
using System.Windows.Data;

namespace CCSpotViewer.ViewModels;

[ValueConversion(typeof(bool),typeof(string))]
public sealed class BoolToStatusConverter:IValueConverter {
    public static readonly BoolToStatusConverter Instance = new();

    public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
        => value is true ? "Connected" : "Disconnected";

    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
        => throw new NotSupportedException();
}