using System;
using Xunit;
using Moq;
using LiveWordXml.Wpf.Services;

namespace LiveWordXml.Tests.Services
{
    public class TextMatchingServiceTests
    {
        [Fact]
        public void Constructor_ValidDocumentService_ShouldCreateInstance()
        {
            // Arrange
            var mockDocumentService = new Mock<DocumentService>();

            // Act
            var service = new TextMatchingService(mockDocumentService.Object);

            // Assert
            Assert.NotNull(service);
        }

        [Fact]
        public void MatchTextToXml_EmptyText_ShouldReturnEmptyList()
        {
            // Arrange
            var mockDocumentService = new Mock<DocumentService>();
            var service = new TextMatchingService(mockDocumentService.Object);
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