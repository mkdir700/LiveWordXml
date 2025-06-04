using System;
using System.Windows;

namespace LiveWordXml.Wpf.Services
{
    public class ClipboardService
    {
        public void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to copy text to clipboard: {ex.Message}", ex);
            }
        }

        public string GetFromClipboard()
        {
            try
            {
                return Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get text from clipboard: {ex.Message}", ex);
            }
        }
    }
}
