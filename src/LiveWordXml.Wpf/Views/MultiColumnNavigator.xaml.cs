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

        // Column level tracking for depth management
        private readonly Dictionary<int, Border> _columnsByLevel = [];
        private readonly Dictionary<Border, int> _levelsByColumn = [];

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

            // Find the column level for dynamic columns using tracking dictionary
            foreach (var column in _dynamicColumns)
            {
                if (
                    column.Child is Grid grid
                    && grid.Children.OfType<ListBox>().FirstOrDefault() == listBox
                )
                {
                    // Use the tracking dictionary to get the exact level
                    if (_levelsByColumn.TryGetValue(column, out var level))
                    {
                        return level;
                    }
                }
            }

            return 0;
        }

        private void UpdateNavigationPath(DocumentStructureNode selectedNode, int columnLevel)
        {
            System.Diagnostics.Debug.WriteLine(
                $"UpdateNavigationPath: Node '{selectedNode.Name}' at level {columnLevel}, Current path length: {_navigationPath.Count}"
            );

            // Ensure the path is exactly the right length for the current level
            // This handles both forward navigation and branch switching
            while (_navigationPath.Count > columnLevel)
            {
                var removedNode = _navigationPath[^1];
                _navigationPath.RemoveAt(_navigationPath.Count - 1);
                System.Diagnostics.Debug.WriteLine(
                    $"UpdateNavigationPath: Removed '{removedNode.Name}' from path"
                );
            }

            // Add or update current selection
            if (_navigationPath.Count == columnLevel)
            {
                _navigationPath.Add(selectedNode);
                System.Diagnostics.Debug.WriteLine(
                    $"UpdateNavigationPath: Added '{selectedNode.Name}' to path at level {columnLevel}"
                );
            }
            else if (columnLevel < _navigationPath.Count)
            {
                var oldNode = _navigationPath[columnLevel];
                _navigationPath[columnLevel] = selectedNode;
                System.Diagnostics.Debug.WriteLine(
                    $"UpdateNavigationPath: Replaced '{oldNode.Name}' with '{selectedNode.Name}' at level {columnLevel}"
                );
            }

            // Validate path consistency
            ValidateNavigationPath();
        }

        /// <summary>
        /// Validate that the navigation path is consistent with the current column structure
        /// </summary>
        private void ValidateNavigationPath()
        {
            System.Diagnostics.Debug.WriteLine(
                $"ValidateNavigationPath: Path has {_navigationPath.Count} nodes, Columns: {_columnsByLevel.Count}"
            );

            for (int i = 0; i < _navigationPath.Count; i++)
            {
                var node = _navigationPath[i];
                System.Diagnostics.Debug.WriteLine($"  Level {i}: {node.Name} ({node.NodeType})");
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

            // Always remove columns from this level and after
            // This ensures that when switching branches at any level,
            // all deeper levels are properly cleared
            System.Diagnostics.Debug.WriteLine(
                $"CreateOrUpdateColumn: Removing all columns from level {columnLevel} and after"
            );
            RemoveColumnsFromLevel(columnLevel);

            // Create new column
            var column = CreateColumn(nodes, headerText, columnLevel);

            // Add to containers and tracking dictionaries
            _dynamicColumns.Add(column);
            ColumnsContainer.Children.Add(column);

            // Track the column by level
            _columnsByLevel[columnLevel] = column;
            _levelsByColumn[column] = columnLevel;

            System.Diagnostics.Debug.WriteLine(
                $"CreateOrUpdateColumn: Added column at level {columnLevel}. Total dynamic columns: {_dynamicColumns.Count}"
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
                var column = _dynamicColumns[i];
                RemoveColumnFromTracking(column);
                ColumnsContainer.Children.Remove(column);
                _dynamicColumns.RemoveAt(i);
            }
        }

        private void RemoveColumnsFromLevel(int columnLevel)
        {
            // Remove dynamic columns from the specified level and after
            // We need to check each column's actual level, not just the index
            var columnsToRemove = new List<Border>();

            foreach (var column in _dynamicColumns)
            {
                if (_levelsByColumn.TryGetValue(column, out var level) && level >= columnLevel)
                {
                    columnsToRemove.Add(column);
                }
            }

            // Remove the identified columns
            foreach (var column in columnsToRemove)
            {
                RemoveColumnFromTracking(column);
                ColumnsContainer.Children.Remove(column);
                _dynamicColumns.Remove(column);
            }

            System.Diagnostics.Debug.WriteLine(
                $"RemoveColumnsFromLevel: Removed {columnsToRemove.Count} columns from level {columnLevel} and after"
            );
        }

        private void RemoveColumnFromTracking(Border column)
        {
            // Remove column from tracking dictionaries
            if (_levelsByColumn.TryGetValue(column, out var level))
            {
                _levelsByColumn.Remove(column);
                _columnsByLevel.Remove(level);
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

            // Clear tracking dictionaries
            _columnsByLevel.Clear();
            _levelsByColumn.Clear();

            // Reset selection
            SelectedNode = null;
            SelectedRootNode = null;
        }

        private void ScrollToShowNewColumn()
        {
            // Auto-scroll to show the rightmost column
            ColumnsScrollViewer.ScrollToRightEnd();
        }

        /// <summary>
        /// Clear selection in all columns except the specified level
        /// </summary>
        private void ClearSelectionExcept(int exceptLevel)
        {
            // Clear root column selection if not the exception
            if (exceptLevel != 0)
            {
                RootNodesList.SelectedItem = null;
            }

            // Clear dynamic column selections
            foreach (var kvp in _columnsByLevel)
            {
                var level = kvp.Key;
                var column = kvp.Value;

                if (level != exceptLevel && column.Child is Grid grid)
                {
                    var listBox = grid.Children.OfType<ListBox>().FirstOrDefault();
                    if (listBox != null)
                    {
                        listBox.SelectedItem = null;
                    }
                }
            }
        }

        /// <summary>
        /// Get the ListBox for a specific column level
        /// </summary>
        private ListBox GetListBoxForLevel(int level)
        {
            if (level == 0)
                return RootNodesList;

            if (_columnsByLevel.TryGetValue(level, out var column) && column.Child is Grid grid)
            {
                return grid.Children.OfType<ListBox>().FirstOrDefault() ?? RootNodesList;
            }

            return RootNodesList;
        }

        // Public Methods
        public void NavigateToNode(DocumentStructureNode targetNode)
        {
            // Implementation for programmatic navigation
            // This would build the column path to reach the target node
        }

        public List<DocumentStructureNode> GetNavigationPath()
        {
            return [.. _navigationPath];
        }
    }
}
