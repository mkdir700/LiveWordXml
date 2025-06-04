using System.Windows;
using LiveWordXml.Wpf.ViewModels;

namespace LiveWordXml.Wpf
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;

        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainViewModel();
            DataContext = viewModel;
            
            // Subscribe to scroll to search event
            viewModel.OnScrollToSearchRequested += () =>
            {
                XmlPreviewEditor.ScrollToSearchText();
            };
        }

        protected override void OnClosed(System.EventArgs e)
        {
            ViewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
