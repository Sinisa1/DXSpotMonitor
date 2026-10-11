using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace DXSpotMonitor;
/*
 

 dotnet build -f net9.0-android -t:Run
 dotnet build -f net9.0-android
 dotnet clean -f net9.0-android



 # Full clean
Remove-Item -Recurse -Force bin, obj -ErrorAction SilentlyContinue
dotnet restore
dotnet build -f net9.0-android -t:Run

 * */
public partial class MainWindow:Window {
    public MainWindow() {
        InitializeComponent();
    }

    private void Window_Closing(object sender,System.ComponentModel.CancelEventArgs e) {
        // Dispose the ViewModel to flush and close the log file
        if(DataContext is IDisposable disposable) {
            disposable.Dispose();
        }
    }
}

// Value Converters
public class BoolToColorConverter:IValueConverter {
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
        if(value is bool isTrue && isTrue)
            return Colors.Green;
        return Colors.Red;
    }

    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
        throw new NotImplementedException();
    }
}

public class InverseBoolConverter:IValueConverter {
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
        return value is bool b && !b;
    }

    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
        return value is bool b && !b;
    }
}

public class BoolToStatusConverter:IValueConverter {
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
        var param = parameter as string;
        if(value is bool b) {
            if(param == "Log")
                return b ? "Logging" : "Log Off";
            return b ? "✓" : "✗";
        }
        return "✗";
    }

    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
        throw new NotImplementedException();
    }
}

public class BoolToMonitoringStatusConverter:IValueConverter {
    public object Convert(object value,Type targetType,object parameter,CultureInfo culture) {
        return value is bool b && b ? "● MONITORING ACTIVE" : "○ Monitoring Stopped";
    }

    public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture) {
        throw new NotImplementedException();
    }
}