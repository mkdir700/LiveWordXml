using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace LiveWordXml.Wpf.Models
{
    /// <summary>
    /// 表示文档结构树中的一个节点
    /// </summary>
    public class DocumentStructureNode : ObservableObject
    {
        private bool _isExpanded = true;
        private bool _isSelected;
        private bool _isHighlighted;

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
        /// 父节点引用
        /// </summary>
        public DocumentStructureNode? Parent { get; set; }

        /// <summary>
        /// 子节点集合
        /// </summary>
        public ObservableCollection<DocumentStructureNode> Children { get; } = new();

        /// <summary>
        /// 节点是否展开
        /// </summary>
        public bool IsExpanded
        {
            get => _isExpanded;
            set => SetProperty(ref _isExpanded, value);
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
            set => SetProperty(ref _isHighlighted, value);
        }

        /// <summary>
        /// 是否有子节点
        /// </summary>
        public bool HasChildren => Children.Count > 0;

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
                    _ => "📄"
                };
            }
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

            var comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            // 检查当前节点是否匹配
            if (Name.Contains(searchText, comparison) ||
                TextPreview.Contains(searchText, comparison) ||
                NodeType.Contains(searchText, comparison))
            {
                results.Add(this);
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

        public override string ToString()
        {
            return $"{NodeType}: {Name}";
        }
    }
}