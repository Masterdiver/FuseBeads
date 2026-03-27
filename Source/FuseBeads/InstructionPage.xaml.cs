using FuseBeads.ViewModels;
#if ANDROID
using AndroidX.Core.View;
#endif

namespace FuseBeads;

public partial class InstructionPage
{
    public InstructionPage(InstructionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        Loaded += (_, _) => ApplySafeAreaPadding();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplySafeAreaPadding();
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
}
