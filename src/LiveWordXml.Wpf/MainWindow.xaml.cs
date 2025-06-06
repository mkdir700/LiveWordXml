using System.Windows;
using System.Windows.Controls;
using LiveWordXml.Wpf.ViewModels;
using LiveWordXml.Wpf.Models;

namespace LiveWordXml.Wpf
{
    public partial class MainWindow : Window
    {
        private MainViewModel ViewModel => (MainViewModel)DataContext;
        private bool _isXmlPreviewFullscreen = false;

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

        /// <summary>
        /// 处理 XML Preview 全屏切换
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void ButtonFullscreen_Click(object sender, RoutedEventArgs e)
        {
            _isXmlPreviewFullscreen = !_isXmlPreviewFullscreen;
            
            if (_isXmlPreviewFullscreen)
            {
                // 进入全屏模式
                MatchesPanel.Visibility = Visibility.Collapsed;
                Splitter1.Visibility = Visibility.Collapsed;
                StructurePanel.Visibility = Visibility.Collapsed;
                Splitter2.Visibility = Visibility.Collapsed;
                
                // XML Preview 占据所有列
                XmlPreviewPanel.SetValue(Grid.ColumnProperty, 0);
                XmlPreviewPanel.SetValue(Grid.ColumnSpanProperty, 5);
                
                // 更新按钮图标
                ButtonFullscreen.ToolTip = "Exit Fullscreen";
                ((TextBlock)ButtonFullscreen.Content).Text = (string)FindResource("ExitFullScreenIcon");
            }
            else
            {
                // 退出全屏模式
                MatchesPanel.Visibility = Visibility.Visible;
                Splitter1.Visibility = Visibility.Visible;
                StructurePanel.Visibility = ViewModel.IsStructureTreeVisible ? Visibility.Visible : Visibility.Collapsed;
                Splitter2.Visibility = Visibility.Visible;
                
                // 恢复 XML Preview 原始位置
                XmlPreviewPanel.SetValue(Grid.ColumnProperty, 4);
                XmlPreviewPanel.SetValue(Grid.ColumnSpanProperty, 1);
                
                // 更新按钮图标
                ButtonFullscreen.ToolTip = "Toggle Fullscreen XML Preview";
                ((TextBlock)ButtonFullscreen.Content).Text = (string)FindResource("FullScreenIcon");
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            ViewModel?.Dispose();
            base.OnClosed(e);
        }
    }
}
