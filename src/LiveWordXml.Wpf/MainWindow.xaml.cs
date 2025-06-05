using System.Windows;
using System.Windows.Controls;
using LiveWordXml.Wpf.ViewModels;
using LiveWordXml.Wpf.Models;

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

        /// <summary>
        /// 处理文档结构树视图的选择变化事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void DocumentStructureTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is DocumentStructureNode selectedNode)
            {
                ViewModel.SelectedStructureNode = selectedNode;
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            ViewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
