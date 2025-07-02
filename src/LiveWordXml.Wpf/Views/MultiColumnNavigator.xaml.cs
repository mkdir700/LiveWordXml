using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LiveWordXml.Wpf.Models;

namespace LiveWordXml.Wpf.Views
{
    /// <summary>
    /// Multi-column navigator control for hierarchical XML structure navigation
    /// Similar to macOS Finder column view
    /// </summary>
    public partial class MultiColumnNavigator : UserControl
    {
        // Dependency Properties
        public static readonly DependencyProperty RootNodesProperty = DependencyProperty.Register(
            "RootNodes",
            typeof(ObservableCollection<DocumentStructureNode>),
            typeof(MultiColumnNavigator),
            new PropertyMetadata(null, OnRootNodesChanged)
        );

        public static readonly DependencyProperty SelectedNodeProperty =
            DependencyProperty.Register(
                "SelectedNode",
                typeof(DocumentStructureNode),
                typeof(MultiColumnNavigator),
                new PropertyMetadata(null)
            );

        public static readonly DependencyProperty SelectedRootNodeProperty =
            DependencyProperty.Register(
                "SelectedRootNode",
                typeof(DocumentStructureNode),
                typeof(MultiColumnNavigator),
                new PropertyMetadata(null)
            );

        // Events
        public event EventHandler<DocumentStructureNode> NodeSelected;

        // Private fields
        private readonly List<Border> _dynamicColumns = new List<Border>();
        private readonly List<DocumentStructureNode> _navigationPath =
            new List<DocumentStructureNode>();

        public MultiColumnNavigator()
        {
            InitializeComponent();
        }

        // Properties
        public ObservableCollection<DocumentStructureNode> RootNodes
        {
            get => (ObservableCollection<DocumentStructureNode>)GetValue(RootNodesProperty);
            set => SetValue(RootNodesProperty, value);
        }

        public DocumentStructureNode SelectedNode
        {
            get => (DocumentStructureNode)GetValue(SelectedNodeProperty);
            set => SetValue(SelectedNodeProperty, value);
        }

        public DocumentStructureNode SelectedRootNode
        {
            get => (DocumentStructureNode)GetValue(SelectedRootNodeProperty);
            set => SetValue(SelectedRootNodeProperty, value);
        }

        // Event Handlers
        private static void OnRootNodesChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is MultiColumnNavigator navigator)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"MultiColumnNavigator: RootNodes changed. New value: {e.NewValue}"
                );
                if (e.NewValue is ObservableCollection<DocumentStructureNode> nodes)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"MultiColumnNavigator: RootNodes count: {nodes.Count}"
                    );
                    // Directly set the ItemsSource
                    navigator.RootNodesList.ItemsSource = nodes;
                }
                navigator.ResetNavigation();
            }
        }

        private void OnNodeSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (
                sender is ListBox listBox
                && listBox.SelectedItem is DocumentStructureNode selectedNode
            )
            {
                HandleNodeSelection(selectedNode, listBox);
            }
        }

        // Private Methods
        private void HandleNodeSelection(DocumentStructureNode selectedNode, ListBox sourceListBox)
        {
            System.Diagnostics.Debug.WriteLine(
                $"HandleNodeSelection: Node '{selectedNode.Name}', HasChildren: {selectedNode.HasChildren}"
            );

            // Update selected node
            SelectedNode = selectedNode;

            // Determine the column level based on the source ListBox
            int columnLevel = GetColumnLevel(sourceListBox);
            System.Diagnostics.Debug.WriteLine($"HandleNodeSelection: Column level: {columnLevel}");

            // Update navigation path
            UpdateNavigationPath(selectedNode, columnLevel);

            // If node has potential children, load them and create/update next column
            if (selectedNode.HasPotentialChildren)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"HandleNodeSelection: Node has potential children. Current children count: {selectedNode.Children.Count}"
                );

                // Trigger loading of children if needed
                selectedNode.IsExpanded = true;

                // Check again after potential loading
                if (selectedNode.Children?.Any() == true)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"HandleNodeSelection: Creating column for {selectedNode.Children.Count} children"
                    );
                    CreateOrUpdateColumn(columnLevel + 1, selectedNode.Children, selectedNode.Name);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"HandleNodeSelection: No children after loading, removing columns after level {columnLevel}"
                    );
                    RemoveColumnsAfter(columnLevel);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"HandleNodeSelection: No potential children, removing columns after level {columnLevel}"
                );
                // Remove columns after current level
                RemoveColumnsAfter(columnLevel);
            }

            // Raise selection event
            NodeSelected?.Invoke(this, selectedNode);

            // Auto-scroll to show the new column
            ScrollToShowNewColumn();
        }

        private int GetColumnLevel(ListBox listBox)
        {
            if (listBox == RootNodesList)
                return 0;

            // Find the column level for dynamic columns
            for (int i = 0; i < _dynamicColumns.Count; i++)
            {
                if (
                    _dynamicColumns[i].Child is Grid grid
                    && grid.Children.OfType<ListBox>().FirstOrDefault() == listBox
                )
                {
                    return i + 1;
                }
            }

            return 0;
        }

        private void UpdateNavigationPath(DocumentStructureNode selectedNode, int columnLevel)
        {
            // Trim path to current level
            if (_navigationPath.Count > columnLevel)
            {
                _navigationPath.RemoveRange(columnLevel, _navigationPath.Count - columnLevel);
            }

            // Add or update current selection
            if (_navigationPath.Count == columnLevel)
            {
                _navigationPath.Add(selectedNode);
            }
            else if (_navigationPath.Count > columnLevel)
            {
                _navigationPath[columnLevel] = selectedNode;
            }
        }

        private void CreateOrUpdateColumn(
            int columnLevel,
            ObservableCollection<DocumentStructureNode> nodes,
            string headerText
        )
        {
            System.Diagnostics.Debug.WriteLine(
                $"CreateOrUpdateColumn: Level {columnLevel}, Header '{headerText}', Nodes count: {nodes.Count}"
            );

            // Remove columns after this level
            RemoveColumnsAfter(columnLevel - 1);

            // Create new column
            var column = CreateColumn(nodes, headerText, columnLevel);

            // Add to containers
            _dynamicColumns.Add(column);
            ColumnsContainer.Children.Add(column);

            System.Diagnostics.Debug.WriteLine(
                $"CreateOrUpdateColumn: Added column. Total dynamic columns: {_dynamicColumns.Count}"
            );
        }

        private Border CreateColumn(
            ObservableCollection<DocumentStructureNode> nodes,
            string headerText,
            int columnLevel
        )
        {
            var column = new Border
            {
                Style = (Style)FindResource("NavigationColumnStyle"),
                Width = 250,
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(
                new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }
            );

            // Header
            var header = new Border { Style = (Style)FindResource("ColumnHeaderStyle") };
            var headerText_block = new TextBlock
            {
                Text = headerText,
                FontWeight = FontWeights.SemiBold,
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
            };
            header.Child = headerText_block;
            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // ListBox
            var listBox = new ListBox
            {
                ItemsSource = nodes,
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                ItemContainerStyle = (Style)FindResource("NodeItemStyle"),
                ItemTemplate = (DataTemplate)FindResource("NodeItemTemplate"),
            };
            listBox.SelectionChanged += OnNodeSelectionChanged;
            ScrollViewer.SetHorizontalScrollBarVisibility(listBox, ScrollBarVisibility.Disabled);
            Grid.SetRow(listBox, 1);
            grid.Children.Add(listBox);

            column.Child = grid;
            return column;
        }

        private void RemoveColumnsAfter(int columnLevel)
        {
            // Remove dynamic columns after the specified level
            for (int i = _dynamicColumns.Count - 1; i > columnLevel; i--)
            {
                ColumnsContainer.Children.Remove(_dynamicColumns[i]);
                _dynamicColumns.RemoveAt(i);
            }
        }

        private void ResetNavigation()
        {
            // Clear all dynamic columns
            foreach (var column in _dynamicColumns)
            {
                ColumnsContainer.Children.Remove(column);
            }
            _dynamicColumns.Clear();
            _navigationPath.Clear();

            // Reset selection
            SelectedNode = null;
            SelectedRootNode = null;
        }

        private void ScrollToShowNewColumn()
        {
            // Auto-scroll to show the rightmost column
            ColumnsScrollViewer.ScrollToRightEnd();
        }

        // Public Methods
        public void NavigateToNode(DocumentStructureNode targetNode)
        {
            // Implementation for programmatic navigation
            // This would build the column path to reach the target node
        }

        public List<DocumentStructureNode> GetNavigationPath()
        {
            return new List<DocumentStructureNode>(_navigationPath);
        }
    }
}
