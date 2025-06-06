using System;
using System.Windows;

namespace LiveWordXml.Wpf
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Add global exception handling
            DispatcherUnhandledException += (sender, args) =>
            {
                MessageBox.Show($"Unhandled exception: {args.Exception.Message}\n\nStack trace:\n{args.Exception.StackTrace}", 
                    "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
                args.Handled = true;
            };
            
            try
            {
                base.OnStartup(e);
                
                // Create and show the main window
                var mainWindow = new MainWindow();
                mainWindow.Show();
                
                // Ensure the window is visible
                mainWindow.Activate();
                mainWindow.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during startup: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", 
                    "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
