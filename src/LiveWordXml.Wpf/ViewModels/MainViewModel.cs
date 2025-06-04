using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveWordXml.Wpf.Models;
using LiveWordXml.Wpf.Services;

namespace LiveWordXml.Wpf.ViewModels
{
    public class MainViewModel : ObservableObject
    {
        private readonly DocumentService _documentService;
        private readonly ClipboardService _clipboardService;
        private readonly NotificationService _notificationService;

        private string _selectedText = string.Empty;
        private string _documentStatus = "No document loaded";
        private string _statusMessage = "Ready";
        private string _selectedXml = string.Empty;
        private bool _isFormattedXml = true;
        private int _currentMatchIndex = -1;
        private MatchedElement _selectedMatch; public MainViewModel()
        {
            _documentService = new DocumentService();
            _clipboardService = new ClipboardService();
            _notificationService = new NotificationService();

            MatchedElements = new ObservableCollection<MatchedElement>();

            // Commands
            LoadDocumentCommand = new AsyncRelayCommand(LoadDocumentAsync);
            RefreshDocumentCommand = new AsyncRelayCommand(RefreshDocumentAsync);
            PreviousCommand = new RelayCommand(NavigatePrevious, CanNavigatePrevious);
            NextCommand = new RelayCommand(NavigateNext, CanNavigateNext);
            CopyXmlCommand = new RelayCommand(CopyXml, () => !string.IsNullOrEmpty(SelectedXml));
            SaveXmlCommand = new AsyncRelayCommand(SaveXmlAsync, () => !string.IsNullOrEmpty(SelectedXml));
        }

        // Properties
        public string SelectedText
        {
            get => _selectedText;
            set
            {
                if (SetProperty(ref _selectedText, value))
                {
                    _ = ProcessSelectedTextAsync();
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
        }        // Commands
        public IAsyncRelayCommand LoadDocumentCommand { get; }
        public IAsyncRelayCommand RefreshDocumentCommand { get; }
        public IRelayCommand PreviousCommand { get; }
        public IRelayCommand NextCommand { get; }
        public IRelayCommand CopyXmlCommand { get; }
        public IAsyncRelayCommand SaveXmlCommand { get; }

        // Methods
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
                }; if (openFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() => _documentService.LoadDocument(openFileDialog.FileName));

                    DocumentStatus = $"Document loaded: {System.IO.Path.GetFileName(openFileDialog.FileName)}";
                    StatusMessage = "Document loaded successfully";
                    OnPropertyChanged(nameof(IsDocumentLoaded));

                    _notificationService.ShowNotification("Document loaded successfully!");

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

        private async Task ProcessSelectedTextAsync()
        {
            if (string.IsNullOrWhiteSpace(SelectedText) || !_documentService.IsDocumentLoaded)
            {
                MatchedElements.Clear();
                SelectedXml = string.Empty;
                OnPropertyChanged(nameof(MatchCount));
                OnPropertyChanged(nameof(CurrentMatchInfo));
                return;
            }

            try
            {
                StatusMessage = "Searching for matches...";

                var textMatchingService = new TextMatchingService(_documentService);
                var matches = await Task.Run(() => textMatchingService.MatchTextToXml(SelectedText));

                MatchedElements.Clear();

                for (int i = 0; i < matches.Count; i++)
                {
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
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error processing text: {ex.Message}";
                _notificationService.ShowError($"Error processing text: {ex.Message}");
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
    }
}
