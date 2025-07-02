using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        private DocumentStructureNode? _cachedStructure;
        private string? _cachedFilePath;
        private DateTime _cacheTimestamp;

        public DocumentStructureService(DocumentService documentService)
        {
            _documentService =
                documentService ?? throw new ArgumentNullException(nameof(documentService));
        }

        /// <summary>
        /// 构建文档结构树（同步版本，保持向后兼容）
        /// </summary>
        /// <returns>文档结构根节点</returns>
        public DocumentStructureNode BuildDocumentStructure()
        {
            return BuildDocumentStructureAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步构建文档结构树，支持取消和性能监控
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progress">进度报告</param>
        /// <returns>文档结构根节点</returns>
        public async Task<DocumentStructureNode> BuildDocumentStructureAsync(
            CancellationToken cancellationToken = default,
            IProgress<string>? progress = null
        )
        {
            var performanceStopwatch = PerformanceMonitor.StartOperation("BuildDocumentStructure");
            progress?.Report("Starting document structure analysis...");

            try
            {
                // Check cache first
                if (IsCacheValid())
                {
                    progress?.Report("Using cached document structure");
                    PerformanceMonitor.EndOperation("BuildDocumentStructure", performanceStopwatch);
                    return _cachedStructure!;
                }

                if (!_documentService.IsDocumentLoaded)
                {
                    throw new InvalidOperationException("No document is loaded.");
                }

                var body = _documentService.GetDocumentBody();
                if (body == null)
                {
                    throw new InvalidOperationException("Unable to access document body.");
                }

                progress?.Report("Creating document structure tree...");

                // 创建根节点
                var rootNode = new DocumentStructureNode
                {
                    Name = "Document",
                    NodeType = "Document",
                    XPath = "/w:document",
                    Level = 0,
                    TextPreview = "Word Document Root",
                    IsExpanded = true, // Root node should be expanded by default
                };

                // 创建Body节点
                var bodyNode = new DocumentStructureNode
                {
                    Name = "Document Body",
                    NodeType = "Body",
                    XPath = "/w:document/w:body",
                    XmlContent = body.OuterXml,
                    Level = 1,
                    TextPreview = "Document content container",
                    IsExpanded = true, // Body node should be expanded by default
                };

                rootNode.AddChild(bodyNode);

                // 异步解析Body的子元素（使用延迟加载策略）
                await ParseBodyElementsWithLazyLoadingAsync(
                    body,
                    bodyNode,
                    cancellationToken,
                    progress
                );

                // Update cache
                UpdateCache(rootNode);

                PerformanceMonitor.EndOperation("BuildDocumentStructure", performanceStopwatch);
                progress?.Report(
                    $"Document structure built successfully in {performanceStopwatch.ElapsedMilliseconds}ms"
                );

                return rootNode;
            }
            catch (OperationCanceledException)
            {
                progress?.Report("Document structure building cancelled");
                PerformanceMonitor.EndOperation("BuildDocumentStructure", performanceStopwatch);
                throw;
            }
            catch (Exception ex)
            {
                progress?.Report($"Error building document structure: {ex.Message}");
                PerformanceMonitor.EndOperation("BuildDocumentStructure", performanceStopwatch);
                throw;
            }
        }

        /// <summary>
        /// 检查缓存是否有效
        /// </summary>
        /// <returns>缓存是否有效</returns>
        private bool IsCacheValid()
        {
            if (_cachedStructure == null || _cachedFilePath != _documentService.LoadedFilePath)
            {
                return false;
            }

            // Check if file has been modified since cache was created
            try
            {
                var fileInfo = new System.IO.FileInfo(_documentService.LoadedFilePath);
                return fileInfo.LastWriteTime <= _cacheTimestamp;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 更新缓存
        /// </summary>
        /// <param name="structure">文档结构</param>
        private void UpdateCache(DocumentStructureNode structure)
        {
            _cachedStructure = structure;
            _cachedFilePath = _documentService.LoadedFilePath;
            _cacheTimestamp = DateTime.Now;
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _cachedStructure = null;
            _cachedFilePath = null;
            _cacheTimestamp = default;
        }

        /// <summary>
        /// 使用延迟加载策略异步解析Body元素的子元素
        /// </summary>
        /// <param name="body">Body元素</param>
        /// <param name="parentNode">父节点</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progress">进度报告</param>
        private async Task ParseBodyElementsWithLazyLoadingAsync(
            Body body,
            DocumentStructureNode parentNode,
            CancellationToken cancellationToken,
            IProgress<string>? progress
        )
        {
            var elements = body.Elements().ToList();
            var totalElements = elements.Count;
            var processedElements = 0;
            var batchSize = 50; // Process in larger batches for better performance
            var nodeBatch = new List<DocumentStructureNode>();

            // Only load the first level immediately, set up lazy loading for deeper levels
            for (int i = 0; i < elements.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var element = elements[i];
                var elementIndex = i + 1;
                var node = CreateNodeFromElement(element, parentNode.Level + 1, elementIndex);

                if (node != null)
                {
                    nodeBatch.Add(node);

                    // Set up lazy loading for child elements instead of loading them immediately
                    if (element.HasChildren && node.Level < 8) // Use node.Level for correct depth check
                    {
                        var capturedElement = element; // Capture for closure
                        var capturedNode = node;
                        node.SetChildrenLoader(() =>
                            LoadChildrenSynchronously(capturedElement, capturedNode)
                        );
                    }
                }

                processedElements++;

                // Add nodes in batches to reduce UI update frequency
                if (nodeBatch.Count >= batchSize || i == elements.Count - 1)
                {
                    parentNode.AddChildren(nodeBatch);
                    nodeBatch.Clear();

                    progress?.Report($"Processed {processedElements}/{totalElements} elements...");
                    await Task.Yield(); // Allow UI to update
                }
            }
        }

        /// <summary>
        /// 异步解析Body元素的子元素（原始版本，保持向后兼容）
        /// </summary>
        /// <param name="body">Body元素</param>
        /// <param name="parentNode">父节点</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progress">进度报告</param>
        private async Task ParseBodyElementsAsync(
            Body body,
            DocumentStructureNode parentNode,
            CancellationToken cancellationToken,
            IProgress<string>? progress
        )
        {
            var elements = body.Elements().ToList();
            var totalElements = elements.Count;
            var processedElements = 0;

            for (int i = 0; i < elements.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var element = elements[i];
                var elementIndex = i + 1;
                var node = CreateNodeFromElement(element, parentNode.Level + 1, elementIndex);

                if (node != null)
                {
                    parentNode.AddChild(node);

                    // 递归解析子元素（限制深度以避免性能问题）
                    if (parentNode.Level < 10) // Limit recursion depth
                    {
                        await ParseChildElementsAsync(element, node, cancellationToken, progress);
                    }
                }

                processedElements++;
                if (processedElements % 50 == 0) // Report progress every 50 elements
                {
                    progress?.Report($"Processed {processedElements}/{totalElements} elements...");
                    await Task.Yield(); // Allow UI to update
                }
            }
        }

        /// <summary>
        /// 同步加载子节点（用于延迟加载）
        /// </summary>
        /// <param name="element">父元素</param>
        /// <param name="parentNode">父节点</param>
        /// <returns>子节点列表</returns>
        private List<DocumentStructureNode> LoadChildrenSynchronously(
            OpenXmlElement element,
            DocumentStructureNode parentNode
        )
        {
            var children = new List<DocumentStructureNode>();
            var childElements = element.Elements().ToList();

            for (int i = 0; i < childElements.Count; i++)
            {
                var childElement = childElements[i];
                var childIndex = i + 1;
                var childNode = CreateNodeFromElement(
                    childElement,
                    parentNode.Level + 1,
                    childIndex
                );

                if (childNode != null)
                {
                    children.Add(childNode);

                    // Set up lazy loading for grandchildren if they exist
                    if (childElement.HasChildren && childNode.Level < 6) // Use childNode.Level for correct depth check
                    {
                        var capturedChildElement = childElement;
                        var capturedChildNode = childNode;
                        childNode.SetChildrenLoader(() =>
                            LoadChildrenSynchronously(capturedChildElement, capturedChildNode)
                        );
                    }
                }
            }

            return children;
        }

        /// <summary>
        /// 解析Body元素的子元素（同步版本，保持向后兼容）
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
        /// 异步递归解析子元素
        /// </summary>
        /// <param name="element">当前元素</param>
        /// <param name="parentNode">父节点</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <param name="progress">进度报告</param>
        private async Task ParseChildElementsAsync(
            OpenXmlElement element,
            DocumentStructureNode parentNode,
            CancellationToken cancellationToken,
            IProgress<string>? progress
        )
        {
            var childElements = element.Elements().ToList();
            var batchSize = 20; // Process in batches to improve responsiveness

            for (int i = 0; i < childElements.Count; i += batchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var batch = childElements.Skip(i).Take(batchSize);
                var childIndex = i;

                foreach (var childElement in batch)
                {
                    childIndex++;
                    var childNode = CreateNodeFromElement(
                        childElement,
                        parentNode.Level + 1,
                        childIndex
                    );
                    if (childNode != null)
                    {
                        parentNode.AddChild(childNode);

                        // 继续递归解析（限制深度）
                        if (parentNode.Level < 8) // Further limit recursion depth
                        {
                            await ParseChildElementsAsync(
                                childElement,
                                childNode,
                                cancellationToken,
                                progress
                            );
                        }
                    }
                }

                // Yield control after each batch
                if (i + batchSize < childElements.Count)
                {
                    await Task.Yield();
                }
            }
        }

        /// <summary>
        /// 递归解析子元素（同步版本，保持向后兼容）
        /// </summary>
        /// <param name="element">当前元素</param>
        /// <param name="parentNode">父节点</param>
        private void ParseChildElements(OpenXmlElement element, DocumentStructureNode parentNode)
        {
            int childIndex = 0;

            foreach (var childElement in element.Elements())
            {
                childIndex++;
                var childNode = CreateNodeFromElement(
                    childElement,
                    parentNode.Level + 1,
                    childIndex
                );
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
        private DocumentStructureNode? CreateNodeFromElement(
            OpenXmlElement element,
            int level,
            int index
        )
        {
            if (element == null)
                return null;

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
                Level = level,
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
                _ => element.LocalName,
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
                Paragraph p => $"Paragraph {index}"
                    + (string.IsNullOrWhiteSpace(text) ? "" : $": {text}"),
                Run r => $"Run {index}" + (string.IsNullOrWhiteSpace(text) ? "" : $": {text}"),
                Text t => $"Text: {text}",
                Table => $"Table {index}",
                TableRow tr => $"Row {index}",
                TableCell tc => $"Cell {index}"
                    + (string.IsNullOrWhiteSpace(text) ? "" : $": {text}"),
                Hyperlink hl => $"Hyperlink {index}"
                    + (string.IsNullOrWhiteSpace(text) ? "" : $": {text}"),
                BookmarkStart bs => $"Bookmark: {bs.Name?.Value ?? "Unnamed"}",
                Drawing => $"Drawing {index}",
                Picture => $"Picture {index}",
                Break br => GetBreakTypeName(br),
                _ => $"{typeName} {index}",
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
                var siblings = current
                    .Parent.Elements()
                    .Where(e => e.LocalName == current.LocalName)
                    .ToList();
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
        public List<DocumentStructureNode> FindNodesWithText(
            DocumentStructureNode rootNode,
            string searchText,
            bool ignoreCase = true
        )
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
        private DocumentStructureNode? FindNodeByXPathRecursive(
            DocumentStructureNode node,
            string xpath
        )
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
        public DocumentStructureNode? FindNodeByXmlContent(
            DocumentStructureNode rootNode,
            string xmlContent
        )
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
        private DocumentStructureNode? FindNodeByXmlContentRecursive(
            DocumentStructureNode node,
            string xmlContent
        )
        {
            // 精确匹配XML内容
            if (
                !string.IsNullOrEmpty(node.XmlContent)
                && NormalizeXml(node.XmlContent)
                    .Equals(NormalizeXml(xmlContent), StringComparison.OrdinalIgnoreCase)
            )
            {
                return node;
            }

            // 检查是否包含该XML内容（处理嵌套情况）
            if (
                !string.IsNullOrEmpty(node.XmlContent)
                && NormalizeXml(node.XmlContent)
                    .Contains(NormalizeXml(xmlContent), StringComparison.OrdinalIgnoreCase)
            )
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
            return xml.Replace("\r\n", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("  ", " ")
                .Trim();
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
        private void CollectNodeStatistics(
            DocumentStructureNode node,
            Dictionary<string, int> statistics
        )
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
