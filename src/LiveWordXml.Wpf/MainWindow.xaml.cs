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

            // Subscribe to scroll to structure node event
            viewModel.OnScrollToStructureNodeRequested += ScrollToStructureNode;
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
        /// 滚动到指定的结构节点
        /// </summary>
        /// <param name="targetNode">目标节点</param>
        private void ScrollToStructureNode(DocumentStructureNode targetNode)
        {
            if (targetNode == null)
                return;

            // 延迟执行以确保TreeView已经更新了展开状态
            Dispatcher.BeginInvoke(new System.Action(() =>
            {
                var treeViewItem = FindTreeViewItem(DocumentStructureTreeView, targetNode);
                if (treeViewItem != null)
                {
                    treeViewItem.BringIntoView();
                    treeViewItem.Focus();
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 在TreeView中查找对应的TreeViewItem
        /// </summary>
        /// <param name="treeView">TreeView控件</param>
        /// <param name="targetNode">目标节点</param>
        /// <returns>对应的TreeViewItem，如果未找到则返回null</returns>
        private TreeViewItem? FindTreeViewItem(TreeView treeView, DocumentStructureNode targetNode)
        {
            if (treeView.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
                return null;

            return FindTreeViewItemRecursive(treeView, targetNode);
        }

        /// <summary>
        /// 递归查找TreeViewItem
        /// </summary>
        /// <param name="parent">父控件</param>
        /// <param name="targetNode">目标节点</param>
        /// <returns>对应的TreeViewItem</returns>
        private TreeViewItem? FindTreeViewItemRecursive(ItemsControl parent, DocumentStructureNode targetNode)
        {
            if (parent == null)
                return null;

            foreach (var item in parent.Items)
            {
                var container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (container == null)
                    continue;

                if (item == targetNode)
                {
                    return container;
                }

                // 如果容器是展开的，递归搜索子项
                if (container.IsExpanded)
                {
                    var childResult = FindTreeViewItemRecursive(container, targetNode);
                    if (childResult != null)
                        return childResult;
                }
            }

            return null;
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
