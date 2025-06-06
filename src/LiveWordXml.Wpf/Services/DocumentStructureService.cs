using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using LiveWordXml.Wpf.Models;

namespace LiveWordXml.Wpf.Services
{
    /// <summary>
    /// 文档结构分析服务，用于解析Word文档的层次结构
    /// </summary>
    public class DocumentStructureService
    {
        private readonly DocumentService _documentService;

        public DocumentStructureService(DocumentService documentService)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        }

        /// <summary>
        /// 构建文档结构树
        /// </summary>
        /// <returns>文档结构根节点</returns>
        public DocumentStructureNode BuildDocumentStructure()
        {
            if (!_documentService.IsDocumentLoaded)
            {
                throw new InvalidOperationException("No document is loaded.");
            }

            var body = _documentService.GetDocumentBody();
            if (body == null)
            {
                throw new InvalidOperationException("Unable to access document body.");
            }

            // 创建根节点
            var rootNode = new DocumentStructureNode
            {
                Name = "Document",
                NodeType = "Document",
                XPath = "/w:document",
                Level = 0,
                TextPreview = "Word Document Root"
            };

            // 创建Body节点
            var bodyNode = new DocumentStructureNode
            {
                Name = "Document Body",
                NodeType = "Body",
                XPath = "/w:document/w:body",
                XmlContent = body.OuterXml,
                Level = 1,
                TextPreview = "Document content container"
            };

            rootNode.AddChild(bodyNode);

            // 解析Body的子元素
            ParseBodyElements(body, bodyNode);

            return rootNode;
        }

        /// <summary>
        /// 解析Body元素的子元素
        /// </summary>
        /// <param name="body">Body元素</param>
        /// <param name="parentNode">父节点</param>
        private void ParseBodyElements(Body body, DocumentStructureNode parentNode)
        {
            int elementIndex = 0;

            foreach (var element in body.Elements())
            {
                elementIndex++;
                var node = CreateNodeFromElement(element, parentNode.Level + 1, elementIndex);
                if (node != null)
                {
                    parentNode.AddChild(node);

                    // 递归解析子元素
                    ParseChildElements(element, node);
                }
            }
        }

        /// <summary>
        /// 递归解析子元素
        /// </summary>
        /// <param name="element">当前元素</param>
        /// <param name="parentNode">父节点</param>
        private void ParseChildElements(OpenXmlElement element, DocumentStructureNode parentNode)
        {
            int childIndex = 0;

            foreach (var childElement in element.Elements())
            {
                childIndex++;
                var childNode = CreateNodeFromElement(childElement, parentNode.Level + 1, childIndex);
                if (childNode != null)
                {
                    parentNode.AddChild(childNode);

                    // 继续递归解析
                    ParseChildElements(childElement, childNode);
                }
            }
        }

        /// <summary>
        /// 从OpenXML元素创建结构节点
        /// </summary>
        /// <param name="element">OpenXML元素</param>
        /// <param name="level">层级</param>
        /// <param name="index">在同级中的索引</param>
        /// <returns>文档结构节点</returns>
        private DocumentStructureNode? CreateNodeFromElement(OpenXmlElement element, int level, int index)
        {
            if (element == null) return null;

            var nodeType = GetElementTypeName(element);
            var name = GetElementDisplayName(element, index);
            var textPreview = GetElementTextPreview(element);
            var xpath = GenerateXPath(element);

            var node = new DocumentStructureNode
            {
                Name = name,
                NodeType = nodeType,
                XPath = xpath,
                XmlContent = element.OuterXml,
                TextPreview = textPreview,
                Level = level
            };

            return node;
        }

        /// <summary>
        /// 获取元素类型名称
        /// </summary>
        /// <param name="element">OpenXML元素</param>
        /// <returns>类型名称</returns>
        private string GetElementTypeName(OpenXmlElement element)
        {
            return element switch
            {
                Paragraph => "Paragraph",
                Run => "Run",
                Text => "Text",
                Table => "Table",
                TableRow => "TableRow",
                TableCell => "TableCell",
                Hyperlink => "Hyperlink",
                Drawing => "Drawing",
                Picture => "Picture",
                BookmarkStart => "Bookmark",
                BookmarkEnd => "BookmarkEnd",
                CommentRangeStart => "Comment",
                FieldCode => "Field",
                FieldChar => "FieldChar",
                Break => "Break",
                TabChar => "Tab",
                SectionProperties => "Section",
                ParagraphProperties => "ParagraphProperties",
                RunProperties => "RunProperties",
                _ => element.LocalName
            };
        }

        /// <summary>
        /// 获取元素的显示名称
        /// </summary>
        /// <param name="element">OpenXML元素</param>
        /// <param name="index">索引</param>
        /// <returns>显示名称</returns>
        private string GetElementDisplayName(OpenXmlElement element, int index)
        {
            var typeName = GetElementTypeName(element);
            var text = GetElementTextPreview(element);

            return element switch
            {
                Paragraph p => $"Paragraph {index}" + (string.IsNullOrWhiteSpace(text) ? "" : $": {text}"),
                Run r => $"Run {index}" + (string.IsNullOrWhiteSpace(text) ? "" : $": {text}"),
                Text t => $"Text: {text}",
                Table => $"Table {index}",
                TableRow tr => $"Row {index}",
                TableCell tc => $"Cell {index}" + (string.IsNullOrWhiteSpace(text) ? "" : $": {text}"),
                Hyperlink hl => $"Hyperlink {index}" + (string.IsNullOrWhiteSpace(text) ? "" : $": {text}"),
                BookmarkStart bs => $"Bookmark: {bs.Name?.Value ?? "Unnamed"}",
                Drawing => $"Drawing {index}",
                Picture => $"Picture {index}",
                Break br => GetBreakTypeName(br),
                _ => $"{typeName} {index}"
            };
        }

        /// <summary>
        /// 获取换行符类型名称
        /// </summary>
        /// <param name="breakElement">换行元素</param>
        /// <returns>换行类型名称</returns>
        private string GetBreakTypeName(Break breakElement)
        {
            if (breakElement.Type?.Value == BreakValues.Page)
                return "Page Break";
            if (breakElement.Type?.Value == BreakValues.Column)
                return "Column Break";
            if (breakElement.Type?.Value == BreakValues.TextWrapping)
                return "Text Wrapping Break";

            return "Line Break";
        }

        /// <summary>
        /// 获取元素的文本预览
        /// </summary>
        /// <param name="element">OpenXML元素</param>
        /// <returns>文本预览</returns>
        private string GetElementTextPreview(OpenXmlElement element)
        {
            var text = element.InnerText?.Trim() ?? "";

            // 限制预览长度
            if (text.Length > 50)
            {
                text = text.Substring(0, 50) + "...";
            }

            // 替换换行符和多余空格
            text = text.Replace('\r', ' ').Replace('\n', ' ');
            while (text.Contains("  "))
            {
                text = text.Replace("  ", " ");
            }

            return text;
        }

        /// <summary>
        /// 生成元素的XPath
        /// </summary>
        /// <param name="element">OpenXML元素</param>
        /// <returns>XPath字符串</returns>
        private string GenerateXPath(OpenXmlElement element)
        {
            var pathParts = new List<string>();
            var current = element;

            while (current != null && current.Parent != null)
            {
                var elementName = $"w:{current.LocalName}";

                // 计算在同级元素中的位置
                var siblings = current.Parent.Elements().Where(e => e.LocalName == current.LocalName).ToList();
                if (siblings.Count > 1)
                {
                    var index = siblings.IndexOf(current) + 1;
                    elementName += $"[{index}]";
                }

                pathParts.Insert(0, elementName);
                current = current.Parent;
            }

            return "/w:document/w:body/" + string.Join("/", pathParts);
        }

        /// <summary>
        /// 在文档结构中查找包含指定文本的节点
        /// </summary>
        /// <param name="rootNode">根节点</param>
        /// <param name="searchText">搜索文本</param>
        /// <param name="ignoreCase">是否忽略大小写</param>
        /// <returns>匹配的节点列表</returns>
        public List<DocumentStructureNode> FindNodesWithText(DocumentStructureNode rootNode, string searchText, bool ignoreCase = true)
        {
            if (rootNode == null || string.IsNullOrWhiteSpace(searchText))
                return new List<DocumentStructureNode>();

            return rootNode.FindNodes(searchText, ignoreCase);
        }

        /// <summary>
        /// 根据XPath查找节点
        /// </summary>
        /// <param name="rootNode">根节点</param>
        /// <param name="xpath">XPath路径</param>
        /// <returns>匹配的节点，如果未找到则返回null</returns>
        public DocumentStructureNode? FindNodeByXPath(DocumentStructureNode rootNode, string xpath)
        {
            if (rootNode == null || string.IsNullOrWhiteSpace(xpath))
                return null;

            return FindNodeByXPathRecursive(rootNode, xpath);
        }

        /// <summary>
        /// 递归查找XPath匹配的节点
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="xpath">XPath路径</param>
        /// <returns>匹配的节点</returns>
        private DocumentStructureNode? FindNodeByXPathRecursive(DocumentStructureNode node, string xpath)
        {
            if (node.XPath.Equals(xpath, StringComparison.OrdinalIgnoreCase))
                return node;

            foreach (var child in node.Children)
            {
                var result = FindNodeByXPathRecursive(child, xpath);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 根据XML内容查找匹配的节点
        /// </summary>
        /// <param name="rootNode">根节点</param>
        /// <param name="xmlContent">要查找的XML内容</param>
        /// <returns>匹配的节点，如果未找到则返回null</returns>
        public DocumentStructureNode? FindNodeByXmlContent(DocumentStructureNode rootNode, string xmlContent)
        {
            if (rootNode == null || string.IsNullOrWhiteSpace(xmlContent))
                return null;

            return FindNodeByXmlContentRecursive(rootNode, xmlContent.Trim());
        }

        /// <summary>
        /// 递归查找XML内容匹配的节点
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="xmlContent">XML内容</param>
        /// <returns>匹配的节点</returns>
        private DocumentStructureNode? FindNodeByXmlContentRecursive(DocumentStructureNode node, string xmlContent)
        {
            // 精确匹配XML内容
            if (!string.IsNullOrEmpty(node.XmlContent) && 
                NormalizeXml(node.XmlContent).Equals(NormalizeXml(xmlContent), StringComparison.OrdinalIgnoreCase))
            {
                return node;
            }

            // 检查是否包含该XML内容（处理嵌套情况）
            if (!string.IsNullOrEmpty(node.XmlContent) && 
                NormalizeXml(node.XmlContent).Contains(NormalizeXml(xmlContent), StringComparison.OrdinalIgnoreCase))
            {
                // 首先尝试在子节点中找到更精确的匹配
                foreach (var child in node.Children)
                {
                    var childResult = FindNodeByXmlContentRecursive(child, xmlContent);
                    if (childResult != null)
                        return childResult;
                }
                
                // 如果子节点中没有找到更精确的匹配，返回当前节点
                return node;
            }

            // 继续在子节点中搜索
            foreach (var child in node.Children)
            {
                var result = FindNodeByXmlContentRecursive(child, xmlContent);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        /// 标准化XML内容，移除空白字符以便比较
        /// </summary>
        /// <param name="xml">XML内容</param>
        /// <returns>标准化后的XML</returns>
        private string NormalizeXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return string.Empty;

            // 移除多余的空白字符和换行符
            return xml.Replace("\r\n", "").Replace("\n", "").Replace("\r", "")
                     .Replace("  ", " ").Trim();
        }

        /// <summary>
        /// 获取节点统计信息
        /// </summary>
        /// <param name="rootNode">根节点</param>
        /// <returns>统计信息字典</returns>
        public Dictionary<string, int> GetNodeStatistics(DocumentStructureNode rootNode)
        {
            var statistics = new Dictionary<string, int>();

            if (rootNode == null)
                return statistics;

            CollectNodeStatistics(rootNode, statistics);
            return statistics;
        }

        /// <summary>
        /// 递归收集节点统计信息
        /// </summary>
        /// <param name="node">当前节点</param>
        /// <param name="statistics">统计信息字典</param>
        private void CollectNodeStatistics(DocumentStructureNode node, Dictionary<string, int> statistics)
        {
            if (statistics.ContainsKey(node.NodeType))
                statistics[node.NodeType]++;
            else
                statistics[node.NodeType] = 1;

            foreach (var child in node.Children)
            {
                CollectNodeStatistics(child, statistics);
            }
        }
    }
}