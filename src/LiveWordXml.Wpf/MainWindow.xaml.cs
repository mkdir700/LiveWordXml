using System.Windows;
using LiveWordXml.Wpf.ViewModels;

namespace LiveWordXml.Wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}
