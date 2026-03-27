using FuseBeads.ViewModels;
#if ANDROID
using AndroidX.Core.View;
#endif

namespace FuseBeads;

public partial class MainPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Loaded += (_, _) => ApplySafeAreaPadding();
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(MainViewModel.PatternImageSource))
                ApplyZoom();
        };
    }

    private bool _initialized;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        ApplySafeAreaPadding();
        if (!_initialized && BindingContext is MainViewModel vm)
        {
            _initialized = true;
            await vm.InitializeAsync();
        }
    }

    private void ApplySafeAreaPadding()
    {
        double topInset = 0;
        double bottomInset = 0;

#if ANDROID
        if (Platform.CurrentActivity?.Window?.DecorView is { } decorView)
        {
            var insets = ViewCompat.GetRootWindowInsets(decorView);
            var systemBars = insets?.GetInsets(WindowInsetsCompat.Type.SystemBars());
            if (systemBars is not null)
            {
                var density = DeviceDisplay.MainDisplayInfo.Density;
                topInset = systemBars.Top / density;
                bottomInset = systemBars.Bottom / density;
            }
        }
#elif IOS || MACCATALYST
        foreach (var scene in UIKit.UIApplication.SharedApplication.ConnectedScenes)
        {
            if (scene is UIKit.UIWindowScene windowScene)
            {
                var window = windowScene.Windows.FirstOrDefault(w => w.IsKeyWindow);
                if (window is not null)
                {
                    var safeArea = window.SafeAreaInsets;
                    topInset = safeArea.Top;
                    bottomInset = safeArea.Bottom;
                    break;
                }
            }
        }
#endif

        Padding = new Thickness(0, topInset, 0, bottomInset);
    }

    private double _zoomFactor = 1.0;

    private void OnZoomIn(object? sender, EventArgs e)
    {
        _zoomFactor = Math.Min(_zoomFactor + 0.25, 5.0);
        ApplyZoom();
    }

    private void OnZoomOut(object? sender, EventArgs e)
    {
        _zoomFactor = Math.Max(_zoomFactor - 0.25, 0.25);
        ApplyZoom();
    }

    private void OnZoomReset(object? sender, EventArgs e)
    {
        _zoomFactor = 1.0;
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        if (BindingContext is not MainViewModel vm || !vm.HasPattern) return;

        PatternImage.WidthRequest = vm.GridWidth * vm.BeadSizePx * _zoomFactor;
        PatternImage.HeightRequest = vm.GridHeight * vm.BeadSizePx * _zoomFactor;
        ZoomLabel.Text = $"{_zoomFactor * 100:F0}%";
    }

    private void OnPatternTapped(object? sender, TappedEventArgs e)
    {
        if (BindingContext is not MainViewModel vm) return;
        if (!vm.HasPattern) return;

        var pos = e.GetPosition(PatternImage);
        if (pos is null) return;

        double w = PatternImage.WidthRequest;
        double h = PatternImage.HeightRequest;
        if (w <= 0 || h <= 0) return;

        int col = (int)(pos.Value.X / w * vm.GridWidth);
        int row = (int)(pos.Value.Y / h * vm.GridHeight);

        vm.ToggleBead(row, col);
    }
}
