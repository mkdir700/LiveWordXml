# LiveWordXml

Real-time Word document XML extractor. Select text in any application and instantly get the corresponding XML from your Word document.

## Features

- 🚀 **Real-time extraction** - Get XML instantly when you select text
- 📋 **Clipboard integration** - Automatically copies XML to clipboard
- 🔍 **Smart matching** - Finds exact XML elements for selected text
- 🖥️ **System tray** - Runs quietly in background
- ⚡ **Fast performance** - Optimized for quick response

## Usage

1. Load a Word document (.docx)
2. Select any text in any application
3. XML is automatically copied to clipboard
4. Paste wherever you need it!

## Requirements

# LiveWordXml

Real-time Word document XML extractor. Load a Word document and extract the corresponding XML for any text you paste or select.

## Features

- 🚀 **Real-time extraction** - Get XML instantly when you enter text
- 📋 **Clipboard integration** - Automatically copies XML to clipboard
- 🔍 **Smart matching** - Finds exact XML elements for selected text
- 🖥️ **System tray** - Runs quietly in background with notifications
- ⚡ **Fast performance** - Optimized for quick response
- 📄 **Multiple formats** - Supports paragraphs, runs, and other Word elements

## Usage

1. Launch the LiveWordXml application
2. Click "Load Word Document" and select a .docx file
3. Paste or type text in the "Selected Text" field
4. The corresponding XML will appear in the "Extracted XML" area
5. Click "Copy XML" to copy the result to clipboard
6. Use the XML in your applications!

## Requirements

- .NET 8.0 or higher
- Windows OS
- Word documents in .docx format (OpenXML)

## Building

```bash
# Build the solution
dotnet build

# Run the application
dotnet run --project src/LiveWordXml/LiveWordXml.csproj

# Run tests
dotnet test
```

## Architecture

- **MainForm**: Windows Forms UI for user interaction
- **DocumentService**: Handles Word document loading and XML extraction
- **TextMatchingService**: Matches text to corresponding XML elements
- **ClipboardService**: Manages clipboard operations
- **SystemTrayHelper**: Provides system tray notifications

## License

This project is licensed under the MIT License.
