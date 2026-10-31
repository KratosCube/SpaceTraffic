using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;

namespace SpaceTraffic;

public partial class MainView: UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    public MainView(string scenario) : this()
    {
        DrawingHost.Content = new DrawingPanel(scenario);
    }
}