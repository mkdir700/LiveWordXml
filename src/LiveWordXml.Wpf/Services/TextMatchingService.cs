using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LiveWordXml.Wpf.Services
{
    public class TextMatchingService
    {
        private readonly DocumentService _documentService;

        public TextMatchingService(DocumentService documentService)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
        }

        public List<string> MatchTextToXml(string searchText)
        {
            var matchedElements = new List<string>();

            if (string.IsNullOrWhiteSpace(searchText) || !_documentService.IsDocumentLoaded)
            {
                return matchedElements;
            }

            var body = _documentService.GetDocumentBody();
            if (body == null) return matchedElements;

            searchText = searchText.Trim();

            // Search in paragraphs
            var paragraphs = body.Descendants<Paragraph>();
            foreach (var paragraph in paragraphs)
            {
                if (ContainsText(paragraph.InnerText, searchText))
                {
                    matchedElements.Add(paragraph.OuterXml);
                }
            }

            // Search in runs
            var runs = body.Descendants<Run>();
            foreach (var run in runs)
            {
                if (ContainsText(run.InnerText, searchText))
                {
                    // Only add if not already included as part of a paragraph
                    if (!matchedElements.Any(xml => xml.Contains(run.OuterXml)))
                    {
                        matchedElements.Add(run.OuterXml);
                    }
                }
            }

            // Search in text elements
            var textElements = body.Descendants<Text>();
            foreach (var textElement in textElements)
            {
                if (ContainsText(textElement.InnerText, searchText))
                {
                    // Only add if not already included as part of a larger element
                    if (!matchedElements.Any(xml => xml.Contains(textElement.OuterXml)))
                    {
                        matchedElements.Add(textElement.OuterXml);
                    }
                }
            }

            return matchedElements.Distinct().ToList();
        }

        private static bool ContainsText(string elementText, string searchText)
        {
            if (string.IsNullOrEmpty(elementText) || string.IsNullOrEmpty(searchText))
                return false;

            return elementText.Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }
    }
}
