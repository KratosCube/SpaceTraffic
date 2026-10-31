using Avalonia.Controls;
using System;
using System.IO;

namespace SpaceTraffic
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes new MainWindow without any specified scenario
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            Width = 800;
            Height = 600;

            MinWidth = 800;
            MinHeight = 600;

        }

        /// <summary>
        /// Initializes new MainWindow with specified scenario
        /// </summary>
        /// <param name="scenario">name of the scenario</param>
        public MainWindow(string scenario)
        {
            string path = Path.Combine(
                AppContext.BaseDirectory,
                "data",
                scenario
            );

            if (!File.Exists(path))
            {
                throw new FileNotFoundException(
                    $"Scénář '{scenario}' nebyl nalezen.",
                    path
                );
            }

            InitializeComponent();

            Width = 800;
            Height = 600;
            MinWidth = 800;
            MinHeight = 600;

            ViewHost.Content = new MainView(scenario);
        }
    }
}