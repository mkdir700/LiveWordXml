using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWordXml.Wpf.Models;
using LiveWordXml.Wpf.Services;

namespace LiveWordXml.Wpf.ViewModels
{
    public class MainViewModel : ObservableObject, IDisposable
    {
        private readonly DocumentService _documentService;
        private readonly ClipboardService _clipboardService;
        private readonly NotificationService _notificationService;
        private readonly DocumentStructureService _documentStructureService;

        private string _selectedText = string.Empty;
        private string _documentStatus = "No document loaded";
        private string _statusMessage = "Ready";
        private string _selectedXml = string.Empty;
        private bool _isFormattedXml = true;
        private int _currentMatchIndex = -1;
        private MatchedElement _selectedMatch;
        private string _searchHighlightText = string.Empty;
        private DocumentStructureNode _documentStructure;
        private DocumentStructureNode _selectedStructureNode;
        private bool _isStructureTreeVisible = true;
        
        // Debounce related fields
        private Timer _debounceTimer;
        private readonly int _debounceDelay = 500; // 500ms delay
        private CancellationTokenSource _searchCancellationTokenSource;
        private readonly Dispatcher _dispatcher;

        public MainViewModel()
        {
            _documentService = new DocumentService();
            _clipboardService = new ClipboardService();
            _notificationService = new NotificationService();
            _documentStructureService = new DocumentStructureService(_documentService);
            _dispatcher = Dispatcher.CurrentDispatcher;

            MatchedElements = new ObservableCollection<MatchedElement>();

            // Commands
            LoadDocumentCommand = new AsyncRelayCommand(LoadDocumentAsync);
            RefreshDocumentCommand = new AsyncRelayCommand(RefreshDocumentAsync);
            PreviousCommand = new RelayCommand(NavigatePrevious, CanNavigatePrevious);
            NextCommand = new RelayCommand(NavigateNext, CanNavigateNext);
            CopyXmlCommand = new RelayCommand(CopyXml, () => !string.IsNullOrEmpty(SelectedXml));
            SaveXmlCommand = new AsyncRelayCommand(SaveXmlAsync, () => !string.IsNullOrEmpty(SelectedXml));
            ScrollToSearchCommand = new RelayCommand(() => OnScrollToSearchRequested?.Invoke());
            
            // Document Structure Commands
            ExpandAllCommand = new RelayCommand(ExpandAll, () => DocumentStructure != null);
            CollapseAllCommand = new RelayCommand(CollapseAll, () => DocumentStructure != null);
            RefreshStructureCommand = new AsyncRelayCommand(RefreshDocumentStructureAsync, () => IsDocumentLoaded);
        }

        // Properties
        public string SelectedText
        {
            get => _selectedText;
            set
            {
                if (SetProperty(ref _selectedText, value))
                {
                    SearchHighlightText = value; // Update search highlight text
                    DebouncedProcessSelectedText();
                    HighlightNodesWithText(value);
                }
            }
        }

        public string DocumentStatus
        {
            get => _documentStatus;
            set => SetProperty(ref _documentStatus, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public string SelectedXml
        {
            get => _selectedXml;
            set
            {
                SetProperty(ref _selectedXml, value);
                CopyXmlCommand.NotifyCanExecuteChanged();
                SaveXmlCommand.NotifyCanExecuteChanged();
            }
        }

        public bool IsFormattedXml
        {
            get => _isFormattedXml;
            set
            {
                if (SetProperty(ref _isFormattedXml, value))
                {
                    UpdateSelectedXml();
                }
            }
        }

        public MatchedElement SelectedMatch
        {
            get => _selectedMatch;
            set
            {
                if (SetProperty(ref _selectedMatch, value))
                {
                    if (value != null)
                    {
                        _currentMatchIndex = MatchedElements.IndexOf(value);
                        UpdateSelectedXml();
                        OnPropertyChanged(nameof(CurrentMatchInfo));
                        PreviousCommand.NotifyCanExecuteChanged();
                        NextCommand.NotifyCanExecuteChanged();
                    }
                }
            }
        }

        public string SearchHighlightText
        {
            get => _searchHighlightText;
            set => SetProperty(ref _searchHighlightText, value);
        }

        public DocumentStructureNode DocumentStructure
        {
            get => _documentStructure;
            set
            {
                SetProperty(ref _documentStructure, value);
                ExpandAllCommand.NotifyCanExecuteChanged();
                CollapseAllCommand.NotifyCanExecuteChanged();
            }
        }

        public DocumentStructureNode SelectedStructureNode
        {
            get => _selectedStructureNode;
            set
            {
                if (SetProperty(ref _selectedStructureNode, value))
                {
                    if (value != null)
                    {
                        // 当选择结构树节点时，显示对应的XML内容
                        SelectedXml = IsFormattedXml
                            ? _documentService.FormatXml(value.XmlContent)
                            : value.XmlContent;
                    }
                }
            }
        }

        public bool IsStructureTreeVisible
        {
            get => _isStructureTreeVisible;
            set => SetProperty(ref _isStructureTreeVisible, value);
        }

        // Event for requesting scroll to search text
        public event Action OnScrollToSearchRequested;
        public ObservableCollection<MatchedElement> MatchedElements { get; }

        public bool IsDocumentLoaded => _documentService.IsDocumentLoaded;

        public int MatchCount => MatchedElements.Count;

        public string CurrentMatchInfo
        {
            get
            {
                if (MatchCount == 0) return "No matches";
                if (_currentMatchIndex >= 0)
                    return $"Match {_currentMatchIndex + 1} of {MatchCount}";
                return $"{MatchCount} matches found";
            }
        }

        // Commands
        public IAsyncRelayCommand LoadDocumentCommand { get; }
        public IAsyncRelayCommand RefreshDocumentCommand { get; }
        public IRelayCommand PreviousCommand { get; }
        public IRelayCommand NextCommand { get; }
        public IRelayCommand CopyXmlCommand { get; }
        public IAsyncRelayCommand SaveXmlCommand { get; }
        public IRelayCommand ScrollToSearchCommand { get; }
        
        // Document Structure Commands
        public IRelayCommand ExpandAllCommand { get; }
        public IRelayCommand CollapseAllCommand { get; }
        public IAsyncRelayCommand RefreshStructureCommand { get; }

        // Methods
        private void DebouncedProcessSelectedText()
        {
            // Cancel previous search if still running
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource = new CancellationTokenSource();
            
            // Reset the debounce timer
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(async _ =>
            {
                try
                {
                    if (!_searchCancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await ProcessSelectedTextAsync(_searchCancellationTokenSource.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when search is cancelled
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error during search: {ex.Message}";
                }
            }, null, _debounceDelay, Timeout.Infinite);
        }

        private async Task LoadDocumentAsync()
        {
            try
            {
                StatusMessage = "Loading document...";

                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx|All files (*.*)|*.*",
                    FilterIndex = 1,
                    RestoreDirectory = true
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => _documentService.LoadDocument(openFileDialog.FileName));

                    DocumentStatus = $"Document loaded: {System.IO.Path.GetFileName(openFileDialog.FileName)}";
                    StatusMessage = "Document loaded successfully";
                    OnPropertyChanged(nameof(IsDocumentLoaded));
                    RefreshStructureCommand.NotifyCanExecuteChanged();

                    _notificationService.ShowNotification("Document loaded successfully!");

                    // Build document structure
                    await BuildDocumentStructureAsync();

                    // Process current text if any
                    if (!string.IsNullOrEmpty(SelectedText))
                    {
                        await ProcessSelectedTextAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading document: {ex.Message}";
                _notificationService.ShowError($"Error loading document: {ex.Message}");
            }
        }

        private async Task RefreshDocumentAsync()
        {
            if (!_documentService.IsDocumentLoaded)
            {
                StatusMessage = "No document loaded to refresh";
                return;
            }

            try
            {
                StatusMessage = "Refreshing document...";

                await Task.Run(() => _documentService.RefreshDocument());

                var fileName = System.IO.Path.GetFileName(_documentService.LoadedFilePath);
                DocumentStatus = $"Document refreshed: {fileName}";
                StatusMessage = "Document refreshed successfully";

                _notificationService.ShowNotification("Document refreshed with latest content!");

                // Rebuild document structure
                await BuildDocumentStructureAsync();

                // Re-process current text to get updated matches
                if (!string.IsNullOrEmpty(SelectedText))
                {
                    await ProcessSelectedTextAsync();
                }
                else
                {
                    // Clear matches if no text is selected
                    MatchedElements.Clear();
                    SelectedXml = string.Empty;
                    OnPropertyChanged(nameof(MatchCount));
                    OnPropertyChanged(nameof(CurrentMatchInfo));
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error refreshing document: {ex.Message}";
                _notificationService.ShowError($"Error refreshing document: {ex.Message}");
            }
        }

        private async Task BuildDocumentStructureAsync()
        {
            try
            {
                StatusMessage = "Building document structure...";

                var structure = await Task.Run(() => _documentStructureService.BuildDocumentStructure());
                
                await _dispatcher.InvokeAsync(() =>
                {
                    DocumentStructure = structure;
                    StatusMessage = "Document structure built successfully";
                });
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error building document structure: {ex.Message}";
                _notificationService.ShowError($"Error building document structure: {ex.Message}");
            }
        }

        private async Task RefreshDocumentStructureAsync()
        {
            if (!_documentService.IsDocumentLoaded)
            {
                StatusMessage = "No document loaded";
                return;
            }

            await BuildDocumentStructureAsync();
        }

        private void ExpandAll()
        {
            DocumentStructure?.ExpandAll();
        }

        private void CollapseAll()
        {
            DocumentStructure?.CollapseAll();
        }

        private void HighlightNodesWithText(string searchText)
        {
            if (DocumentStructure == null || string.IsNullOrWhiteSpace(searchText))
            {
                ClearHighlights();
                return;
            }

            var matchingNodes = _documentStructureService.FindNodesWithText(DocumentStructure, searchText);
            
            // Clear previous highlights
            ClearHighlights();
            
            // Highlight matching nodes
            foreach (var node in matchingNodes)
            {
                node.IsHighlighted = true;
                node.ExpandToNode(); // Expand path to highlighted node
            }
        }

        private void ClearHighlights()
        {
            if (DocumentStructure != null)
            {
                ClearHighlightsRecursive(DocumentStructure);
            }
        }

        private void ClearHighlightsRecursive(DocumentStructureNode node)
        {
            node.IsHighlighted = false;
            foreach (var child in node.Children)
            {
                ClearHighlightsRecursive(child);
            }
        }

        private async Task ProcessSelectedTextAsync(CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(SelectedText) || !_documentService.IsDocumentLoaded)
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    MatchedElements.Clear();
                    SelectedXml = string.Empty;
                    OnPropertyChanged(nameof(MatchCount));
                    OnPropertyChanged(nameof(CurrentMatchInfo));
                });
                return;
            }

            try
            {
                await _dispatcher.InvokeAsync(() => StatusMessage = "Searching for matches...");

                var textMatchingService = new TextMatchingService(_documentService);
                var currentText = SelectedText; // Capture current text value
                var matches = await Task.Run(() => textMatchingService.MatchTextToXml(currentText), cancellationToken);

                // Check if operation was cancelled after async operation
                cancellationToken.ThrowIfCancellationRequested();

                // Update UI on the main thread
                await _dispatcher.InvokeAsync(() =>
                {
                    MatchedElements.Clear();

                    for (int i = 0; i < matches.Count; i++)
                    {
                        // Check cancellation during loop
                        if (cancellationToken.IsCancellationRequested)
                            return;
                        
                        var element = new MatchedElement
                        {
                            Index = i + 1,
                            XmlContent = matches[i],
                            ElementType = ExtractElementType(matches[i]),
                            Preview = CreatePreview(matches[i])
                        };
                        MatchedElements.Add(element);
                    }

                    OnPropertyChanged(nameof(MatchCount));
                    OnPropertyChanged(nameof(CurrentMatchInfo));

                    if (MatchedElements.Any())
                    {
                        SelectedMatch = MatchedElements.First();
                        StatusMessage = $"Found {MatchCount} matching elements";
                        _notificationService.ShowNotification($"Found {MatchCount} matching XML elements");
                    }
                    else
                    {
                        StatusMessage = "No matching elements found";
                        SelectedXml = string.Empty;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Expected when search is cancelled, don't show error
                await _dispatcher.InvokeAsync(() => StatusMessage = "Search cancelled");
            }
            catch (Exception ex)
            {
                await _dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Error processing text: {ex.Message}";
                    _notificationService.ShowError($"Error processing text: {ex.Message}");
                });
            }
        }

        private void UpdateSelectedXml()
        {
            if (SelectedMatch != null)
            {
                SelectedXml = IsFormattedXml
                    ? _documentService.FormatXml(SelectedMatch.XmlContent)
                    : SelectedMatch.XmlContent;
            }
            else if (SelectedStructureNode != null)
            {
                SelectedXml = IsFormattedXml
                    ? _documentService.FormatXml(SelectedStructureNode.XmlContent)
                    : SelectedStructureNode.XmlContent;
            }
        }

        private void NavigatePrevious()
        {
            if (CanNavigatePrevious())
            {
                SelectedMatch = MatchedElements[_currentMatchIndex - 1];
            }
        }

        private void NavigateNext()
        {
            if (CanNavigateNext())
            {
                SelectedMatch = MatchedElements[_currentMatchIndex + 1];
            }
        }

        private bool CanNavigatePrevious() => _currentMatchIndex > 0;
        private bool CanNavigateNext() => _currentMatchIndex < MatchCount - 1;

        private void CopyXml()
        {
            if (!string.IsNullOrEmpty(SelectedXml))
            {
                _clipboardService.CopyToClipboard(SelectedXml);
                StatusMessage = "XML copied to clipboard";
                _notificationService.ShowNotification("XML copied to clipboard!");
            }
        }

        private async Task SaveXmlAsync()
        {
            if (string.IsNullOrEmpty(SelectedXml)) return;

            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "XML files (*.xml)|*.xml|Text files (*.txt)|*.txt|All files (*.*)|*.*",
                    FilterIndex = 1,
                    DefaultExt = "xml"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await System.IO.File.WriteAllTextAsync(saveFileDialog.FileName, SelectedXml);
                    StatusMessage = $"XML saved to {System.IO.Path.GetFileName(saveFileDialog.FileName)}";
                    _notificationService.ShowNotification("XML saved successfully!");
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving XML: {ex.Message}";
                _notificationService.ShowError($"Error saving XML: {ex.Message}");
            }
        }

        private static string ExtractElementType(string xml)
        {
            // Extract element type from XML
            var start = xml.IndexOf('<');
            if (start >= 0)
            {
                var end = xml.IndexOfAny(new[] { ' ', '>', '\r', '\n' }, start + 1);
                if (end > start)
                {
                    return xml.Substring(start + 1, end - start - 1);
                }
            }
            return "Unknown";
        }

        private static string CreatePreview(string xml)
        {
            // Create a short preview of the XML content
            var preview = xml.Replace("\r\n", " ").Replace("\n", " ").Trim();
            return preview.Length > 60 ? preview.Substring(0, 60) + "..." : preview;
        }

        public void Dispose()
        {
            _debounceTimer?.Dispose();
            _searchCancellationTokenSource?.Cancel();
            _searchCancellationTokenSource?.Dispose();
        }
    }
}
