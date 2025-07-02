using System.Windows;
using System.Windows.Controls;
using LiveWordXml.Wpf.Models;
using LiveWordXml.Wpf.ViewModels;

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

            // Subscribe to scroll to structure node event
            viewModel.OnScrollToStructureNodeRequested += ScrollToStructureNode;
        }

        /// <summary>
        /// 处理多列导航器的节点选择事件
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="selectedNode">选中的节点</param>
        private void OnNavigatorNodeSelected(object sender, DocumentStructureNode selectedNode)
        {
            ViewModel.SelectedStructureNode = selectedNode;
        }

        /// <summary>
        /// 处理文档结构树视图的选择变化事件（保留用于兼容性）
        /// </summary>
        /// <param name="sender">事件发送者</param>
        /// <param name="e">事件参数</param>
        private void DocumentStructureTreeView_SelectedItemChanged(
            object sender,
            RoutedPropertyChangedEventArgs<object> e
        )
        {
            if (e.NewValue is DocumentStructureNode selectedNode)
            {
                ViewModel.SelectedStructureNode = selectedNode;
            }
        }

        /// <summary>
        /// 滚动到指定的结构节点
        /// </summary>
        /// <param name="targetNode">目标节点</param>
        private void ScrollToStructureNode(DocumentStructureNode targetNode)
        {
            if (targetNode == null)
                return;

            // 使用多列导航器导航到指定节点
            DocumentNavigator.NavigateToNode(targetNode);
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
                NavigationPanel.Visibility = Visibility.Collapsed;
                MainSplitter.Visibility = Visibility.Collapsed;

                // XML Preview 占据所有列
                XmlPreviewPanel.SetValue(Grid.ColumnProperty, 0);
                XmlPreviewPanel.SetValue(Grid.ColumnSpanProperty, 3);

                // 更新按钮图标
                ButtonFullscreen.ToolTip = "Exit Fullscreen";
                ((TextBlock)ButtonFullscreen.Content).Text = (string)FindResource(
                    "ExitFullScreenIcon"
                );
            }
            else
            {
                // 退出全屏模式
                NavigationPanel.Visibility = Visibility.Visible;
                MainSplitter.Visibility = Visibility.Visible;

                // 恢复 XML Preview 原始位置
                XmlPreviewPanel.SetValue(Grid.ColumnProperty, 2);
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
