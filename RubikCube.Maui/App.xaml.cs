namespace RubikCube.Maui;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = new Window(new MainPage()) { Title = "Rubik's Cube 4D Solver" };
        //builder.ConfigureLifecycleEvents(events => {
        //    events.AddWindows(lifecycle => {
        //        lifecycle.OnWindowCreated(window => {
        //            // Remove the default WinUI background
        //            Microsoft.UI.Xaml.Application.Current.Resources["NavigationViewContentBackground"] =
        //                new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
        //        });
        //    });
        //});
        return window;
    }
}
