using System.Globalization;

namespace DXSpotViewer.Converters;

/// <summary>
/// Converts connection status boolean to color
/// </summary>
public class BoolToColorConverter:IValueConverter {
    public object Convert(object? value,Type targetType,object? parameter,CultureInfo culture) {
        if(value is bool isConnected) {
            return isConnected ? Colors.Green : Colors.Gray;
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value,Type targetType,object? parameter,CultureInfo culture) {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Inverts a boolean value
/// </summary>
public class InvertedBoolConverter:IValueConverter {
    public object Convert(object? value,Type targetType,object? parameter,CultureInfo culture) {
        if(value is bool b) {
            return !b;
        }
        return true;
    }

    public object? ConvertBack(object? value,Type targetType,object? parameter,CultureInfo culture) {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Returns true if string is not null or whitespace
/// </summary>
public class StringNotEmptyConverter:IValueConverter {
    public object Convert(object? value,Type targetType,object? parameter,CultureInfo culture) {
        return value is string str && !string.IsNullOrWhiteSpace(str);
    }

    // ✅ FIX: Match IValueConverter interface signature exactly
    public object? ConvertBack(object? value,Type targetType,object? parameter,CultureInfo culture) {
        throw new NotImplementedException();
    }
}