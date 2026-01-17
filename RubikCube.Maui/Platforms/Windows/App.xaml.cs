using Microsoft.UI.Xaml;

namespace RubikCube.Maui.WinUI;

public partial class App : MauiWinUIApplication
{
    public App()
    {
        this.InitializeComponent();
    }

    protected override MauiApp CreateMauiApp() => RubikCube.Maui.MauiProgram.CreateMauiApp();
}
