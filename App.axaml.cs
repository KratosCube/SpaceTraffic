using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using System.IO;

namespace SpaceTraffic
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                //process the parameters
                var scenario = "default";
                if (desktop.Args != null && desktop.Args.Length > 0)
                {
                    scenario = desktop.Args[0];
                }

                desktop.MainWindow = new MainWindow(scenario);
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}