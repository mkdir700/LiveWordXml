using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Search;

namespace LiveWordXml.Wpf.Views
{
    /// <summary>
    /// XML Editor with syntax highlighting, line numbers, folding, and search highlighting
    /// </summary>
    public partial class XmlEditor : UserControl
    {
        private readonly FoldingManager _foldingManager;
        private readonly XmlFoldingStrategy _foldingStrategy;
        private readonly SearchResultBackgroundRenderer _searchRenderer;
        private string _searchText = string.Empty;
        
        public static readonly DependencyProperty XmlTextProperty =
            DependencyProperty.Register(nameof(XmlText), typeof(string), typeof(XmlEditor),
                new PropertyMetadata(string.Empty, OnXmlTextChanged));

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(XmlEditor),
                new PropertyMetadata(string.Empty, OnSearchTextChanged));

        public string XmlText
        {
            get => (string)GetValue(XmlTextProperty);
            set => SetValue(XmlTextProperty, value);
        }

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public XmlEditor()
        {
            InitializeComponent();
            
            // Set up XML syntax highlighting
            XmlTextEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("XML");
            
            // Set up folding
            _foldingManager = FoldingManager.Install(XmlTextEditor.TextArea);
            _foldingStrategy = new XmlFoldingStrategy();
            
            // Set up search highlighting
            _searchRenderer = new SearchResultBackgroundRenderer();
            XmlTextEditor.TextArea.TextView.BackgroundRenderers.Add(_searchRenderer);
            
            // Install search panel
            SearchPanel.Install(XmlTextEditor);
            
            // Set up document change handler for folding updates
            XmlTextEditor.TextChanged += (s, e) => UpdateFolding();
        }

        private static void OnXmlTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is XmlEditor editor && e.NewValue is string newText)
            {
                editor.XmlTextEditor.Document.Text = newText;
                editor.UpdateFolding();
                editor.UpdateSearchHighlighting();
            }
        }

        private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is XmlEditor editor && e.NewValue is string newSearchText)
            {
                editor._searchText = newSearchText;
                editor.UpdateSearchHighlighting();
            }
        }

        private void UpdateFolding()
        {
            if (_foldingManager != null && _foldingStrategy != null)
            {
                var newFoldings = _foldingStrategy.CreateNewFoldings(XmlTextEditor.Document, out int firstErrorOffset);
                _foldingManager.UpdateFoldings(newFoldings, firstErrorOffset);
            }
        }

        private void UpdateSearchHighlighting()
        {
            _searchRenderer.ClearResults();
            
            if (string.IsNullOrEmpty(_searchText) || string.IsNullOrEmpty(XmlTextEditor.Document.Text))
                return;

            var text = XmlTextEditor.Document.Text;
            var searchPattern = Regex.Escape(_searchText);
            var regex = new Regex(searchPattern, RegexOptions.IgnoreCase);
            
            foreach (Match match in regex.Matches(text))
            {
                var segment = new TextSegment { StartOffset = match.Index, Length = match.Length };
                _searchRenderer.AddResult(segment);
            }

            XmlTextEditor.TextArea.TextView.InvalidateLayer(KnownLayer.Background);
        }

        /// <summary>
        /// Scroll to and highlight the first occurrence of the search text
        /// </summary>
        public void ScrollToSearchText()
        {
            if (string.IsNullOrEmpty(_searchText) || string.IsNullOrEmpty(XmlTextEditor.Document.Text))
                return;

            var text = XmlTextEditor.Document.Text;
            var index = text.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase);
            
            if (index >= 0)
            {
                var location = XmlTextEditor.Document.GetLocation(index);
                XmlTextEditor.ScrollToLine(location.Line);
                XmlTextEditor.Select(index, _searchText.Length);
            }
        }
    }

    /// <summary>
    /// Background renderer for highlighting search results
    /// </summary>
    public class SearchResultBackgroundRenderer : IBackgroundRenderer
    {
        private readonly List<TextSegment> _results = new List<TextSegment>();
        private readonly Brush _backgroundBrush = new SolidColorBrush(Color.FromArgb(100, 255, 255, 0)); // Semi-transparent yellow

        public KnownLayer Layer => KnownLayer.Background;

        public void AddResult(TextSegment segment)
        {
            _results.Add(segment);
        }

        public void ClearResults()
        {
            _results.Clear();
        }

        public void Draw(TextView textView, DrawingContext drawingContext)
        {
            if (_results.Count == 0)
                return;

            foreach (var result in _results)
            {
                var geometries = BackgroundGeometryBuilder.GetRectsForSegment(textView, result);
                foreach (var geometry in geometries)
                {
                    drawingContext.DrawRectangle(_backgroundBrush, null, geometry);
                }
            }
        }
    }

    /// <summary>
    /// XML folding strategy for collapsing XML elements
    /// </summary>
    public class XmlFoldingStrategy
    {
        public IEnumerable<NewFolding> CreateNewFoldings(TextDocument document, out int firstErrorOffset)
        {
            firstErrorOffset = -1;
            var foldings = new List<NewFolding>();
            
            try
            {
                var text = document.Text;
                CreateFoldingsFromXml(foldings, text);
            }
            catch (Exception)
            {
                // Ignore XML parsing errors for folding
            }
            
            foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return foldings;
        }

        private void CreateFoldingsFromXml(List<NewFolding> foldings, string xml)
        {
            // Simple regex-based folding for XML elements
            var tagPattern = @"<(\w+)(?:\s[^>]*)?>.*?</\1>";
            var regex = new Regex(tagPattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            
            foreach (Match match in regex.Matches(xml))
            {
                if (match.Length > 50) // Only fold elements with substantial content
                {
                    var lines = xml.Substring(0, match.Index).Count(c => c == '\n');
                    var endLines = xml.Substring(0, match.Index + match.Length).Count(c => c == '\n');
                    
                    if (endLines > lines) // Multi-line element
                    {
                        foldings.Add(new NewFolding(match.Index, match.Index + match.Length)
                        {
                            Name = $"<{GetTagName(match.Value)}...>"
                        });
                    }
                }
            }
        }

        private string GetTagName(string xmlElement)
        {
            var match = Regex.Match(xmlElement, @"<(\w+)");
            return match.Success ? match.Groups[1].Value : "element";
        }
    }
} 