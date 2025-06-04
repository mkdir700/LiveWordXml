using System;
using System.IO;
using Xunit;
using LiveWordXml.Wpf.Services;

namespace LiveWordXml.Tests.Services
{
    public class DocumentServiceTests
    {
        private readonly DocumentService _documentService;

        public DocumentServiceTests()
        {
            _documentService = new DocumentService();
        }

        [Fact]
        public void LoadDocument_InvalidFilePath_ShouldThrowException()
        {
            // Arrange
            var invalidFilePath = "path/to/invalid/document.docx";

            // Act & Assert
            Assert.Throws<FileNotFoundException>(() => _documentService.LoadDocument(invalidFilePath));
        }

        [Fact]
        public void ExtractXml_NoDocumentLoaded_ShouldThrowException()
        {
            // Arrange - no document loaded

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => _documentService.ExtractXml());
        }

        // Note: Tests for valid documents would require actual Word document files
        // These would be integration tests that require test files to be present
    }
}