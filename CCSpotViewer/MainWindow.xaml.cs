using System.Windows;
using System.Windows.Controls;
using CCSpotViewer.ViewModels;

namespace CCSpotViewer;

public partial class MainWindow:Window {
    public MainWindow() {
        InitializeComponent();

        // Auto-scroll log textboxes when new text arrives
        if(DataContext is MainViewModel vm) {
            vm.PropertyChanged += (_,e) => {
                if(e.PropertyName == nameof(MainViewModel.AppLog))
                    tbAppLog.ScrollToEnd();
                else if(e.PropertyName == nameof(MainViewModel.ErrorLog))
                    tbErrorLog.ScrollToEnd();
            };
        }
    }

    protected override void OnClosed(EventArgs e) {
        (DataContext as MainViewModel)?.Dispose();
        base.OnClosed(e);
    }
}