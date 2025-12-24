using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bridgewater.DropboxLinker.Core.Contracts;
using Bridgewater.DropboxLinker.Core.Dropbox;
using Bridgewater.DropboxLinker.Outlook.Services;
using Microsoft.Office.Interop.Outlook;
using Office = Microsoft.Office.Core;

namespace Bridgewater.DropboxLinker.Outlook
{
    /// <summary>
    /// Ribbon extension for the Bridgewater Dropbox Linker.
    /// </summary>
    [ComVisible(true)]
    public class BridgewaterRibbon : Office.IRibbonExtensibility
    {
        private readonly ThisAddIn _addIn;
        private Office.IRibbonUI? _ribbon;

        /// <summary>
        /// Initializes a new instance of the <see cref="BridgewaterRibbon"/> class.
        /// </summary>
        /// <param name="addIn">The parent add-in instance.</param>
        public BridgewaterRibbon(ThisAddIn addIn)
        {
            _addIn = addIn ?? throw new ArgumentNullException(nameof(addIn));
        }

        /// <summary>
        /// Returns the ribbon XML for the custom UI.
        /// </summary>
        public string GetCustomUI(string ribbonId)
        {
            // Load the embedded ribbon XML
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Bridgewater.DropboxLinker.Outlook.Ribbon.BridgewaterRibbon.xml";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                // Fallback to file-based loading during development
                var xmlPath = Path.Combine(
                    Path.GetDirectoryName(assembly.Location) ?? "",
                    "Ribbon", "BridgewaterRibbon.xml");
                
                if (File.Exists(xmlPath))
                {
                    return File.ReadAllText(xmlPath);
                }

                throw new InvalidOperationException("Ribbon XML not found.");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Called when the ribbon is loaded.
        /// </summary>
        public void Ribbon_Load(Office.IRibbonUI ribbonUI)
        {
            _ribbon = ribbonUI;
        }

        /// <summary>
        /// Handles the "Dropbox link" button click.
        /// </summary>
        public void OnDropboxLinkClicked(Office.IRibbonControl control)
        {
            try
            {
                _addIn.Logger.Info("Dropbox link button clicked.");

                // Get the active inspector (compose window)
                var inspector = _addIn.Application.ActiveInspector();
                if (inspector == null)
                {
                    ShowError("Please open an email compose window first.");
                    return;
                }

                var mailItem = inspector.CurrentItem as MailItem;
                if (mailItem == null)
                {
                    ShowError("This feature only works in email compose windows.");
                    return;
                }

                // Get Dropbox root folder
                string dropboxRoot;
                try
                {
                    dropboxRoot = _addIn.FolderLocator.GetBusinessDropboxRoot();
                }
                catch (InvalidOperationException ex)
                {
                    ShowError(ex.Message);
                    return;
                }

                // Show file picker
                var files = ShowFilePicker(dropboxRoot);
                if (files == null || files.Length == 0)
                {
                    _addIn.Logger.Info("File selection cancelled.");
                    return;
                }

                // Process files and insert links
                _ = ProcessFilesAsync(mailItem, files);
            }
            catch (System.Exception ex)
            {
                _addIn.Logger.Error(ex, "Error handling Dropbox link click");
                ShowError($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the file picker dialog.
        /// </summary>
        private string[]? ShowFilePicker(string dropboxRoot)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Select Dropbox Files",
                InitialDirectory = dropboxRoot,
                Multiselect = true,
                Filter = "All Files (*.*)|*.*",
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                // Validate all selected files are in Dropbox root
                var pathMapper = (DropboxPathMapper)_addIn.PathMapper;
                foreach (var file in dialog.FileNames)
                {
                    if (!pathMapper.IsInsideDropboxRoot(dropboxRoot, file))
                    {
                        ShowError($"File is not in your Dropbox folder:\n{file}");
                        return null;
                    }
                }

                return dialog.FileNames;
            }

            return null;
        }

        /// <summary>
        /// Processes selected files and inserts Dropbox link blocks.
        /// </summary>
        private async Task ProcessFilesAsync(MailItem mailItem, string[] files)
        {
            var entryId = mailItem.EntryID ?? Guid.NewGuid().ToString();
            var htmlBlocks = new System.Text.StringBuilder();
            var hasErrors = false;

            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var conversion = new LinkConversionState
                {
                    FileName = fileName,
                    LocalPath = filePath,
                    Status = ConversionStatus.InProgress
                };
                _addIn.ConversionTracker.AddConversion(entryId, conversion);

                try
                {
                    _addIn.Logger.Info($"Creating link for: {fileName}");

                    var fileInfo = new FileInfo(filePath);
                    var request = new LinkRequest
                    {
                        LocalFilePath = filePath,
                        FileSizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
                        ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7)
                    };

                    var result = await _addIn.LinkService.CreateOrReuseSharedLinkAsync(
                        request, CancellationToken.None);

                    // Build HTML block
                    var html = _addIn.BlockBuilder.BuildHtmlBlock(result, request.FileSizeBytes);
                    
                    if (htmlBlocks.Length > 0)
                    {
                        htmlBlocks.Append("<br><br>"); // Blank line between blocks
                    }
                    htmlBlocks.Append(html);

                    // Update conversion state
                    conversion.Status = ConversionStatus.Success;
                    conversion.ResultUrl = result.Url;

                    _addIn.Logger.Info($"Link created successfully: {result.Url}");
                }
                catch (DropboxApiException ex)
                {
                    hasErrors = true;
                    conversion.Status = ConversionStatus.Failed;
                    conversion.ErrorMessage = ex.Message;
                    _addIn.Logger.Error(ex, $"Dropbox API error for {fileName}");
                }
                catch (System.Exception ex)
                {
                    hasErrors = true;
                    conversion.Status = ConversionStatus.Failed;
                    conversion.ErrorMessage = ex.Message;
                    _addIn.Logger.Error(ex, $"Error creating link for {fileName}");
                }
            }

            // Insert HTML at cursor position
            if (htmlBlocks.Length > 0)
            {
                InsertHtmlAtCursor(mailItem, htmlBlocks.ToString());
            }

            if (hasErrors)
            {
                ShowError("Some files could not be linked. " +
                    "Send will be blocked until you resolve the issues.");
            }
        }

        /// <summary>
        /// Inserts HTML content at the current cursor position.
        /// </summary>
        private void InsertHtmlAtCursor(MailItem mailItem, string html)
        {
            try
            {
                // Ensure HTML format
                if (mailItem.BodyFormat != OlBodyFormat.olFormatHTML)
                {
                    mailItem.BodyFormat = OlBodyFormat.olFormatHTML;
                }

                var inspector = mailItem.GetInspector;
                var wordEditor = inspector.WordEditor as Microsoft.Office.Interop.Word.Document;

                if (wordEditor != null)
                {
                    // Use Word's selection object to insert at cursor
                    var selection = wordEditor.Application.Selection;
                    
                    // Insert the HTML
                    // Word doesn't directly support HTML paste, so we use a workaround:
                    // 1. Copy HTML to clipboard
                    // 2. Paste as HTML
                    
                    var dataObject = new DataObject();
                    dataObject.SetData(DataFormats.Html, FormatHtmlForClipboard(html));
                    dataObject.SetData(DataFormats.Text, StripHtml(html));
                    
                    Clipboard.SetDataObject(dataObject, true);
                    selection.Paste();

                    _addIn.Logger.Info("HTML block inserted at cursor position.");
                }
                else
                {
                    // Fallback: append to end of body
                    mailItem.HTMLBody = mailItem.HTMLBody.Replace(
                        "</body>", 
                        $"<br><br>{html}</body>");
                    
                    _addIn.Logger.Warn("Word editor not available, inserted at end of body.");
                }
            }
            catch (System.Exception ex)
            {
                _addIn.Logger.Error(ex, "Error inserting HTML at cursor");
                
                // Last resort fallback
                try
                {
                    mailItem.HTMLBody = mailItem.HTMLBody.Replace(
                        "</body>",
                        $"<br><br>{html}</body>");
                }
                catch
                {
                    ShowError("Could not insert the Dropbox link block.");
                }
            }
        }

        /// <summary>
        /// Formats HTML for clipboard (adds CF_HTML headers).
        /// </summary>
        private static string FormatHtmlForClipboard(string html)
        {
            // CF_HTML clipboard format requires specific headers
            const string header = "Version:0.9\r\n" +
                "StartHTML:{0:D8}\r\n" +
                "EndHTML:{1:D8}\r\n" +
                "StartFragment:{2:D8}\r\n" +
                "EndFragment:{3:D8}\r\n";

            const string htmlStart = "<html><body><!--StartFragment-->";
            const string htmlEnd = "<!--EndFragment--></body></html>";

            var headerLength = string.Format(header, 0, 0, 0, 0).Length;
            var startHtml = headerLength;
            var startFragment = startHtml + htmlStart.Length;
            var endFragment = startFragment + html.Length;
            var endHtml = endFragment + htmlEnd.Length;

            var formattedHeader = string.Format(header, startHtml, endHtml, startFragment, endFragment);
            return formattedHeader + htmlStart + html + htmlEnd;
        }

        /// <summary>
        /// Strips HTML tags to get plain text.
        /// </summary>
        private static string StripHtml(string html)
        {
            // Simple HTML stripping for fallback
            var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]*>", "");
            text = System.Net.WebUtility.HtmlDecode(text);
            return text.Trim();
        }

        /// <summary>
        /// Shows an error message to the user.
        /// </summary>
        private void ShowError(string message)
        {
            MessageBox.Show(
                message,
                "Bridgewater Dropbox Linker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
