using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace LiveWordXml.Wpf.Services
{
    public class DocumentService : IDisposable
    {
        private string _filePath = string.Empty;
        private WordprocessingDocument? _wordDocument;
        private string? _tempFilePath; // 临时文件路径

        public bool IsDocumentLoaded => !string.IsNullOrEmpty(_filePath) && File.Exists(_filePath);
        public string LoadedFilePath => _filePath;

        public void LoadDocument(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("The specified document was not found.", filePath);
            }

            CloseDocument();
            _filePath = filePath;
        }

        /// <summary>
        /// Refresh the document to get the latest content from disk
        /// </summary>
        public void RefreshDocument()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                throw new InvalidOperationException("No document path is set. Please load a document first.");
            }

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException("The document file no longer exists.", _filePath);
            }

            CloseDocument();
            CleanupTempFile(); // 清理旧的临时文件
        }

        /// <summary>
        /// Get a read-only document instance, opening it fresh each time to get latest content
        /// If the original file is locked, creates a temporary copy and opens that instead
        /// </summary>
        private WordprocessingDocument GetReadOnlyDocument()
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                throw new InvalidOperationException("No document is loaded. Please load a document first.");
            }

            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException("The document file no longer exists.", _filePath);
            }

            string fileToOpen = _filePath;

            try
            {
                // First try to open the original file directly
                var openSettings = new OpenSettings
                {
                    MarkupCompatibilityProcessSettings = new MarkupCompatibilityProcessSettings(
                        MarkupCompatibilityProcessMode.ProcessAllParts,
                        FileFormatVersions.Office2019)
                };

                var document = WordprocessingDocument.Open(fileToOpen, false, openSettings);

                // Validate that we can actually read the document content
                if (document?.MainDocumentPart?.Document != null)
                {
                    return document;
                }

                document?.Dispose();
                throw new InvalidOperationException("Unable to access document content.");
            }
            catch (IOException)
            {
                // If direct access fails due to file lock, try using a temporary copy
                try
                {
                    fileToOpen = CreateTemporaryCopy(_filePath);

                    var openSettings = new OpenSettings
                    {
                        MarkupCompatibilityProcessSettings = new MarkupCompatibilityProcessSettings(
                            MarkupCompatibilityProcessMode.ProcessAllParts,
                            FileFormatVersions.Office2019)
                    };

                    var document = WordprocessingDocument.Open(fileToOpen, false, openSettings);

                    if (document?.MainDocumentPart?.Document == null)
                    {
                        document?.Dispose();
                        throw new InvalidOperationException("Unable to access document content from temporary copy.");
                    }

                    return document;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot open document. File may be locked and unable to create temporary copy: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error opening document: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Creates a temporary copy of the document file for reading when the original is locked
        /// </summary>
        private string CreateTemporaryCopy(string originalFilePath)
        {
            try
            {
                // 清理之前的临时文件
                CleanupTempFile();

                // 创建临时文件路径
                string tempDirectory = Path.GetTempPath();
                string fileName = Path.GetFileNameWithoutExtension(originalFilePath);
                string extension = Path.GetExtension(originalFilePath);
                _tempFilePath = Path.Combine(tempDirectory, $"{fileName}_temp_{DateTime.Now:yyyyMMdd_HHmmss}{extension}");

                // 尝试多种方式复制文件
                if (TryCopyWithFileStream(originalFilePath, _tempFilePath))
                {
                    return _tempFilePath;
                }

                // 如果FileStream失败，尝试使用更宽松的方式
                if (TryCopyWithRetry(originalFilePath, _tempFilePath))
                {
                    return _tempFilePath;
                }

                throw new IOException("Unable to create temporary copy of the document file.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create temporary copy: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 尝试使用FileStream复制文件，允许其他进程共享读写
        /// </summary>
        private bool TryCopyWithFileStream(string source, string destination)
        {
            try
            {
                using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None);
                sourceStream.CopyTo(destStream);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 尝试多次复制文件，有重试机制
        /// </summary>
        private bool TryCopyWithRetry(string source, string destination, int maxRetries = 3)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    // 等待一小段时间后重试
                    if (i > 0)
                    {
                        Thread.Sleep(100 * i); // 递增等待时间
                    }

                    File.Copy(source, destination, true);
                    return true;
                }
                catch (IOException)
                {
                    if (i == maxRetries - 1)
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        private void CleanupTempFile()
        {
            if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
            {
                try
                {
                    File.Delete(_tempFilePath);
                }
                catch
                {
                    // 忽略删除错误，可能是文件仍在使用中
                }
                finally
                {
                    _tempFilePath = null;
                }
            }
        }

        public string ExtractXml()
        {
            using var document = GetReadOnlyDocument();
            if (document?.MainDocumentPart?.Document?.Body == null)
            {
                throw new InvalidOperationException("The document does not contain a valid body.");
            }

            return document.MainDocumentPart.Document.Body.OuterXml;
        }

        public string ExtractRawXml()
        {
            using var document = GetReadOnlyDocument();
            if (document?.MainDocumentPart?.Document?.Body == null)
            {
                throw new InvalidOperationException("The document does not contain a valid body.");
            }

            return document.MainDocumentPart.Document.Body.OuterXml;
        }

        public string GetDocumentText()
        {
            using var document = GetReadOnlyDocument();
            if (document?.MainDocumentPart?.Document?.Body == null)
            {
                throw new InvalidOperationException("The document does not contain a valid body.");
            }

            return document.MainDocumentPart.Document.Body.InnerText;
        }

        public string FormatXml(string xml)
        {
            if (string.IsNullOrEmpty(xml))
                return xml;

            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                var stringBuilder = new StringBuilder();
                var xmlWriterSettings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    NewLineChars = "\r\n",
                    NewLineHandling = NewLineHandling.Replace,
                    OmitXmlDeclaration = true
                };

                using var xmlWriter = XmlWriter.Create(stringBuilder, xmlWriterSettings);
                xmlDoc.Save(xmlWriter);

                return stringBuilder.ToString();
            }
            catch
            {
                return xml; // Return original if formatting fails
            }
        }

        public Body? GetDocumentBody()
        {
            try
            {
                using var document = GetReadOnlyDocument();
                return document?.MainDocumentPart?.Document?.Body;
            }
            catch
            {
                return null;
            }
        }

        public void CloseDocument()
        {
            _wordDocument?.Dispose();
            _wordDocument = null;
            CleanupTempFile(); // 清理临时文件
            // Keep _filePath for refresh functionality
        }

        public void Dispose()
        {
            CloseDocument();
            _filePath = string.Empty;
        }
    }
}
