using System;
using Xunit;
using LiveWordXml.Services;

namespace LiveWordXml.Tests.Services
{
    public class TextMatchingServiceTests
    {
        [Fact]
        public void Constructor_ValidDocumentPath_ShouldCreateInstance()
        {
            // Arrange
            string documentPath = "test.docx";

            // Act
            var service = new TextMatchingService(documentPath);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void MatchTextToXml_EmptyText_ShouldReturnEmptyList()
        {
            // Arrange
            string documentPath = "test.docx";
            var service = new TextMatchingService(documentPath);
            string selectedText = "";

            // Act
            var result = service.MatchTextToXml(selectedText);

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        // Note: Full integration tests would require actual Word document files
        // These would test the actual matching functionality with real documents
    }
}