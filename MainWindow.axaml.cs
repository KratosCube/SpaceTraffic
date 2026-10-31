using Avalonia.Controls;
using System;

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
        }

        /// <summary>
        /// Initializes new MainWindow with specified scenario
        /// </summary>
        /// <param name="scenario">name of the scenario</param>
        public MainWindow(string scenario) : this()
        {
            //TODO: load scenario, or throw an exception if it does not exist
        }
    }
}