using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LiveWordXml.Wpf.Models
{
    /// <summary>
    /// 表示文档结构树中的一个节点
    /// </summary>
    public class DocumentStructureNode : ObservableObject
    {
        private bool _isExpanded = false;
        private bool _isSelected;
        private bool _isHighlighted;
        private bool _hasHighlightedDescendants;
        private bool _childrenLoaded = false;
        private Func<List<DocumentStructureNode>>? _childrenLoader;

        /// <summary>
        /// 节点显示名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点类型（如：Document, Body, Paragraph, Run等）
        /// </summary>
        public string NodeType { get; set; } = string.Empty;

        /// <summary>
        /// 节点的XPath路径，用于定位
        /// </summary>
        public string XPath { get; set; } = string.Empty;

        /// <summary>
        /// 节点对应的XML内容
        /// </summary>
        public string XmlContent { get; set; } = string.Empty;

        /// <summary>
        /// 节点的文本内容预览
        /// </summary>
        public string TextPreview { get; set; } = string.Empty;

        /// <summary>
        /// 节点在文档中的层级深度
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 元素的属性集合（键值对形式存储）
        /// </summary>
        public Dictionary<string, string> Attributes { get; set; } = [];

        /// <summary>
        /// 父节点引用
        /// </summary>
        public DocumentStructureNode? Parent { get; set; }

        /// <summary>
        /// 子节点集合
        /// </summary>
        public ObservableCollection<DocumentStructureNode> Children { get; } = new();

        /// <summary>
        /// 是否有子节点（包括未加载的）
        /// </summary>
        public bool HasPotentialChildren => Children.Count > 0 || _childrenLoader != null;

        /// <summary>
        /// 节点是否展开
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (SetProperty(ref _isExpanded, value) && value)
                {
                    // When expanding, load children if not already loaded
                    LoadChildrenIfNeeded();
                }
            }
        }

        /// <summary>
        /// 节点是否被选中
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        /// <summary>
        /// 节点是否被高亮显示（用于搜索结果）
        /// </summary>
        public bool IsHighlighted
        {
            get => _isHighlighted;
            set
            {
                if (SetProperty(ref _isHighlighted, value))
                {
                    // Update parent nodes' HasHighlightedDescendants property
                    UpdateParentHighlightStatus();
                }
            }
        }

        /// <summary>
        /// 节点的子树是否包含高亮节点（用于搜索结果指示）
        /// </summary>
        public bool HasHighlightedDescendants
        {
            get => _hasHighlightedDescendants;
            set => SetProperty(ref _hasHighlightedDescendants, value);
        }

        /// <summary>
        /// 是否有子节点
        /// </summary>
        public bool HasChildren => Children.Count > 0;

        /// <summary>
        /// 设置子节点加载器（用于延迟加载）
        /// </summary>
        /// <param name="loader">子节点加载函数</param>
        public void SetChildrenLoader(Func<List<DocumentStructureNode>> loader)
        {
            _childrenLoader = loader;
            _childrenLoaded = false;

            // Don't add placeholder immediately - let TreeView show expander based on HasPotentialChildren
            // The placeholder will be added only when expanding if needed

            OnPropertyChanged(nameof(HasPotentialChildren));
        }

        /// <summary>
        /// 如果需要则加载子节点
        /// </summary>
        private void LoadChildrenIfNeeded()
        {
            if (!_childrenLoaded && _childrenLoader != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"Loading children for node: {Name} (Level: {Level})"
                    );

                    // Add a temporary placeholder to show loading state
                    var placeholder = new DocumentStructureNode
                    {
                        Name = "Loading...",
                        NodeType = "Placeholder",
                        Level = Level + 1,
                        Parent = this,
                    };
                    Children.Add(placeholder);

                    // Load the actual children
                    var children = _childrenLoader();
                    System.Diagnostics.Debug.WriteLine($"Loaded {children.Count} children");

                    // Remove the placeholder
                    Children.Remove(placeholder);

                    // Add the real children
                    AddChildren(children);
                    _childrenLoaded = true;
                    _childrenLoader = null; // Clear the loader to free memory

                    // Notify that HasPotentialChildren might have changed
                    OnPropertyChanged(nameof(HasPotentialChildren));

                    System.Diagnostics.Debug.WriteLine(
                        $"Successfully loaded {children.Count} children for {Name}"
                    );
                }
                catch (Exception ex)
                {
                    // Log error but don't crash the UI
                    System.Diagnostics.Debug.WriteLine(
                        $"Error loading children for {Name}: {ex.Message}"
                    );
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                    // Remove any placeholder nodes
                    var placeholders = Children.Where(c => c.NodeType == "Placeholder").ToList();
                    foreach (var placeholder in placeholders)
                    {
                        Children.Remove(placeholder);
                    }

                    // Add error node
                    var errorNode = new DocumentStructureNode
                    {
                        Name = "Error loading children",
                        NodeType = "Error",
                        Level = Level + 1,
                        Parent = this,
                        TextPreview = ex.Message,
                    };
                    Children.Add(errorNode);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Skipping load for {Name}: _childrenLoaded={_childrenLoaded}, _childrenLoader={(_childrenLoader != null ? "not null" : "null")}"
                );
            }
        }

        /// <summary>
        /// 节点的显示图标（根据节点类型）
        /// </summary>
        public string Icon
        {
            get
            {
                return NodeType.ToLower() switch
                {
                    "document" => "📄",
                    "body" => "📝",
                    "paragraph" => "¶",
                    "run" => "📝",
                    "table" => "📊",
                    "tablerow" => "📋",
                    "tablecell" => "🔲",
                    "hyperlink" => "🔗",
                    "drawing" => "🖼️",
                    "picture" => "🖼️",
                    "shape" => "🔷",
                    "textbox" => "📦",
                    "header" => "📋",
                    "footer" => "📋",
                    "footnote" => "📌",
                    "endnote" => "📌",
                    "comment" => "💬",
                    "bookmark" => "🔖",
                    "field" => "🏷️",
                    "equation" => "🧮",
                    "chart" => "📈",
                    _ => "📄",
                };
            }
        }

        /// <summary>
        /// 获取格式化的属性信息字符串
        /// </summary>
        public string AttributesInfo
        {
            get
            {
                if (Attributes == null || Attributes.Count == 0)
                    return "No attributes";

                var attributeStrings = Attributes.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\"");
                return string.Join(", ", attributeStrings);
            }
        }

        /// <summary>
        /// 检查是否有指定的属性
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <returns>是否存在该属性</returns>
        public bool HasAttribute(string attributeName)
        {
            return Attributes?.ContainsKey(attributeName) == true;
        }

        /// <summary>
        /// 获取指定属性的值
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <returns>属性值，如果不存在则返回null</returns>
        public string? GetAttributeValue(string attributeName)
        {
            return Attributes?.TryGetValue(attributeName, out var value) == true ? value : null;
        }

        /// <summary>
        /// 添加子节点
        /// </summary>
        /// <param name="child">要添加的子节点</param>
        public void AddChild(DocumentStructureNode child)
        {
            child.Parent = this;
            child.Level = Level + 1;
            Children.Add(child);
            OnPropertyChanged(nameof(HasChildren));
        }

        /// <summary>
        /// 批量添加子节点（减少UI更新频率）
        /// </summary>
        /// <param name="children">要添加的子节点列表</param>
        public void AddChildren(IEnumerable<DocumentStructureNode> children)
        {
            var childList = children.ToList();
            if (childList.Count == 0)
                return;

            foreach (var child in childList)
            {
                child.Parent = this;
                child.Level = Level + 1;
                Children.Add(child);
            }

            // Only notify once after all children are added
            OnPropertyChanged(nameof(HasChildren));
        }

        /// <summary>
        /// 移除子节点
        /// </summary>
        /// <param name="child">要移除的子节点</param>
        public void RemoveChild(DocumentStructureNode child)
        {
            if (Children.Remove(child))
            {
                child.Parent = null;
                OnPropertyChanged(nameof(HasChildren));
            }
        }

        /// <summary>
        /// 清空所有子节点
        /// </summary>
        public void ClearChildren()
        {
            foreach (var child in Children)
            {
                child.Parent = null;
            }
            Children.Clear();
            OnPropertyChanged(nameof(HasChildren));
        }

        /// <summary>
        /// 获取节点的完整路径（从根节点到当前节点）
        /// </summary>
        /// <returns>节点路径字符串</returns>
        public string GetFullPath()
        {
            if (Parent == null)
                return Name;

            return $"{Parent.GetFullPath()} > {Name}";
        }

        /// <summary>
        /// 递归查找包含指定文本的节点
        /// </summary>
        /// <param name="searchText">要搜索的文本</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns>匹配的节点列表</returns>
        public List<DocumentStructureNode> FindNodes(string searchText, bool ignoreCase = true)
        {
            var results = new List<DocumentStructureNode>();

            if (string.IsNullOrWhiteSpace(searchText))
                return results;

            var comparison = ignoreCase
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            // 检查当前节点是否匹配
            if (
                Name.Contains(searchText, comparison)
                || TextPreview.Contains(searchText, comparison)
                || NodeType.Contains(searchText, comparison)
            )
            {
                results.Add(this);
            }

            // 如果有潜在子节点但还未加载，先加载它们以确保搜索的完整性
            if (HasPotentialChildren && !_childrenLoaded)
            {
                LoadChildrenIfNeeded();
            }

            // 递归搜索子节点
            foreach (var child in Children)
            {
                results.AddRange(child.FindNodes(searchText, ignoreCase));
            }

            return results;
        }

        /// <summary>
        /// 展开到指定节点的路径
        /// </summary>
        public void ExpandToNode()
        {
            var current = Parent;
            while (current != null)
            {
                current.IsExpanded = true;
                current = current.Parent;
            }
        }

        /// <summary>
        /// 递归展开所有子节点
        /// </summary>
        public void ExpandAll()
        {
            IsExpanded = true;
            foreach (var child in Children)
            {
                child.ExpandAll();
            }
        }

        /// <summary>
        /// 递归折叠所有子节点
        /// </summary>
        public void CollapseAll()
        {
            IsExpanded = false;
            foreach (var child in Children)
            {
                child.CollapseAll();
            }
        }

        /// <summary>
        /// Update parent nodes' HasHighlightedDescendants property based on current highlight status
        /// </summary>
        private void UpdateParentHighlightStatus()
        {
            var current = Parent;
            while (current != null)
            {
                var hasHighlightedDescendants = current.HasHighlightedDescendantsRecursive();
                current.HasHighlightedDescendants = hasHighlightedDescendants;
                current = current.Parent;
            }
        }

        /// <summary>
        /// Check if this node or any of its descendants are highlighted
        /// </summary>
        /// <returns>True if this node or any descendant is highlighted</returns>
        private bool HasHighlightedDescendantsRecursive()
        {
            // Check if current node is highlighted
            if (IsHighlighted)
                return true;

            // Check children recursively
            foreach (var child in Children)
            {
                if (child.HasHighlightedDescendantsRecursive())
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Check if any of the direct or indirect children are highlighted
        /// This is used to show the search indicator for nodes that contain search results in their subtree
        /// </summary>
        /// <returns>True if any child or descendant is highlighted</returns>
        private bool HasHighlightedDescendantsInChildren()
        {
            // Check children recursively (but not this node itself)
            foreach (var child in Children)
            {
                if (child.HasHighlightedDescendantsRecursive())
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Clear all highlights in this node and its descendants
        /// </summary>
        public void ClearHighlightsRecursive()
        {
            IsHighlighted = false;
            HasHighlightedDescendants = false;
            foreach (var child in Children)
            {
                child.ClearHighlightsRecursive();
            }
        }

        /// <summary>
        /// Update highlight status for all nodes in the tree
        /// This should be called after setting highlights to ensure parent indicators are correct
        /// </summary>
        public void UpdateHighlightStatusRecursive()
        {
            // Update descendants first
            foreach (var child in Children)
            {
                child.UpdateHighlightStatusRecursive();
            }

            // Update this node's HasHighlightedDescendants
            // Show the indicator if this node has highlighted descendants, regardless of whether this node itself is highlighted
            HasHighlightedDescendants = HasHighlightedDescendantsInChildren();
        }

        public override string ToString()
        {
            return $"{NodeType}: {Name}";
        }
    }
}
