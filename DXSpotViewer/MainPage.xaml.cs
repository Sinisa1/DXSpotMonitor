using DXSpotViewer.ViewModels;

namespace DXSpotViewer;

public partial class MainPage:ContentPage {
    private MainViewModel? _viewModel;

    public MainPage() {
        InitializeComponent();
    }

    protected override void OnAppearing() {
        base.OnAppearing();

        // Create ViewModel only once when page appears
        if(_viewModel == null) {
            try {
                System.Diagnostics.Debug.WriteLine("Creating MainViewModel...");
                _viewModel = new MainViewModel();
                BindingContext = _viewModel;
                System.Diagnostics.Debug.WriteLine("✅ MainPage BindingContext set");
            } catch(Exception ex) {
                System.Diagnostics.Debug.WriteLine($"❌ MainPage ViewModel error: {ex}");
            }
        }
    }
}