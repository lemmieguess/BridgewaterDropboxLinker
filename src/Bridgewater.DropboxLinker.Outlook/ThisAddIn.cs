using System;
using System.IO;
using Bridgewater.DropboxLinker.Core.Auth;
using Bridgewater.DropboxLinker.Core.Contracts;
using Bridgewater.DropboxLinker.Core.Dropbox;
using Bridgewater.DropboxLinker.Core.Html;
using Bridgewater.DropboxLinker.Core.Logging;
using Bridgewater.DropboxLinker.Outlook.Services;
using Microsoft.Office.Interop.Outlook;
using Office = Microsoft.Office.Core;

namespace Bridgewater.DropboxLinker.Outlook
{
    /// <summary>
    /// Main VSTO add-in entry point.
    /// </summary>
    public partial class ThisAddIn
    {
        private IAppLogger _logger = null!;
        private IDropboxFolderLocator _folderLocator = null!;
        private IDropboxPathMapper _pathMapper = null!;
        private IDropboxAuthService _authService = null!;
        private IDropboxLinkService _linkService = null!;
        private ILinkBlockBuilder _blockBuilder = null!;
        private SendGuard _sendGuard = null!;
        private LinkConversionTracker _conversionTracker = null!;

        /// <summary>
        /// Gets the logger instance.
        /// </summary>
        internal IAppLogger Logger => _logger;

        /// <summary>
        /// Gets the folder locator instance.
        /// </summary>
        internal IDropboxFolderLocator FolderLocator => _folderLocator;

        /// <summary>
        /// Gets the path mapper instance.
        /// </summary>
        internal IDropboxPathMapper PathMapper => _pathMapper;

        /// <summary>
        /// Gets the authentication service.
        /// </summary>
        internal IDropboxAuthService AuthService => _authService;

        /// <summary>
        /// Gets the link service.
        /// </summary>
        internal IDropboxLinkService LinkService => _linkService;

        /// <summary>
        /// Gets the block builder.
        /// </summary>
        internal ILinkBlockBuilder BlockBuilder => _blockBuilder;

        /// <summary>
        /// Gets the send guard.
        /// </summary>
        internal SendGuard SendGuard => _sendGuard;

        /// <summary>
        /// Gets the conversion tracker.
        /// </summary>
        internal LinkConversionTracker ConversionTracker => _conversionTracker;

        /// <summary>
        /// Called when the add-in is loaded.
        /// </summary>
        private void ThisAddIn_Startup(object sender, EventArgs e)
        {
            try
            {
                InitializeServices();
                WireUpEvents();
                _logger.Info("Bridgewater Dropbox Linker add-in started successfully.");
            }
            catch (System.Exception ex)
            {
                // Use fallback logging if main logger failed to initialize
                LogStartupError(ex);
            }
        }

        /// <summary>
        /// Called when the add-in is unloaded.
        /// </summary>
        private void ThisAddIn_Shutdown(object sender, EventArgs e)
        {
            try
            {
                UnwireEvents();
                _logger?.Info("Bridgewater Dropbox Linker add-in shutting down.");
                
                // Dispose services
                (_authService as IDisposable)?.Dispose();
                (_linkService as IDisposable)?.Dispose();
                (_logger as IDisposable)?.Dispose();
            }
            catch
            {
                // Swallow shutdown errors
            }
        }

        /// <summary>
        /// Initializes all services.
        /// </summary>
        private void InitializeServices()
        {
            // Set up logging
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Bridgewater", "DropboxLinker", "logs");
            _logger = new FileLogger(logDir);

            // Load configuration
            var config = Configuration.Load();

            // Initialize core services
            _folderLocator = new DropboxFolderLocator();
            _pathMapper = new DropboxPathMapper();

            // Initialize auth and link services
            var tokenStorage = new SecureTokenStorage();
            _authService = new DropboxAuthService(config.DropboxAppKey, tokenStorage, _logger);
            _linkService = new DropboxLinkService(
                _authService, 
                _folderLocator, 
                _pathMapper, 
                _logger,
                config.RootNamespaceId);

            // Initialize UI helpers
            _blockBuilder = new LinkBlockBuilder();
            _sendGuard = new SendGuard();
            _conversionTracker = new LinkConversionTracker();
        }

        /// <summary>
        /// Wires up Outlook event handlers.
        /// </summary>
        private void WireUpEvents()
        {
            Application.ItemSend += Application_ItemSend;
        }

        /// <summary>
        /// Removes Outlook event handlers.
        /// </summary>
        private void UnwireEvents()
        {
            Application.ItemSend -= Application_ItemSend;
        }

        /// <summary>
        /// Handles the ItemSend event to enforce send-time guardrails.
        /// </summary>
        private void Application_ItemSend(object item, ref bool cancel)
        {
            if (!(item is MailItem mailItem))
            {
                return;
            }

            try
            {
                var entryId = mailItem.EntryID ?? "";
                var conversions = _conversionTracker.GetConversions(entryId);
                var result = _sendGuard.Validate(mailItem, conversions);

                if (result.BlockSend)
                {
                    _logger.Warn($"Send blocked: {result.Message}");
                    cancel = true;
                    ShowSendBlockedDialog(result);
                    return;
                }

                if (result.ShowLargeAttachmentWarning)
                {
                    _logger.Info("Large attachment warning triggered.");
                    var proceed = ShowLargeAttachmentWarning(result);
                    if (!proceed)
                    {
                        cancel = true;
                        return;
                    }
                }

                // Clear tracking for sent messages
                _conversionTracker.ClearConversions(entryId);
            }
            catch (System.Exception ex)
            {
                _logger.Error(ex, "Error during send validation");
                // Don't block send on internal errors
            }
        }

        /// <summary>
        /// Shows a dialog when send is blocked due to failed conversions.
        /// </summary>
        private void ShowSendBlockedDialog(SendValidationResult result)
        {
            using var dialog = new SendBlockedDialog(result, _authService, _linkService, _logger);
            dialog.ShowDialog();
        }

        /// <summary>
        /// Shows a warning dialog for large attachments.
        /// </summary>
        /// <returns><c>true</c> if the user wants to proceed; <c>false</c> to cancel.</returns>
        private bool ShowLargeAttachmentWarning(SendValidationResult result)
        {
            var message = "This email contains large attachments:\n\n";
            if (result.LargeAttachments != null)
            {
                foreach (var attachment in result.LargeAttachments)
                {
                    var size = Core.Utilities.ByteSizeFormatter.ToHumanReadable(attachment.SizeBytes);
                    message += $"• {attachment.FileName} ({size})\n";
                }
            }
            message += "\nConsider using Dropbox links instead. Send anyway?";

            var dialogResult = System.Windows.Forms.MessageBox.Show(
                message,
                "Large Attachment Warning",
                System.Windows.Forms.MessageBoxButtons.YesNo,
                System.Windows.Forms.MessageBoxIcon.Warning);

            return dialogResult == System.Windows.Forms.DialogResult.Yes;
        }

        /// <summary>
        /// Logs startup errors to a fallback location.
        /// </summary>
        private void LogStartupError(System.Exception ex)
        {
            try
            {
                var fallbackPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Bridgewater", "DropboxLinker", "startup-error.log");
                Directory.CreateDirectory(Path.GetDirectoryName(fallbackPath)!);
                File.AppendAllText(fallbackPath, 
                    $"{DateTime.Now:O} STARTUP ERROR: {ex}\n");
            }
            catch
            {
                // Last resort - nothing more we can do
            }
        }

        /// <summary>
        /// Creates a custom ribbon for the add-in.
        /// </summary>
        protected override Office.IRibbonExtensibility CreateRibbonExtensibilityObject()
        {
            return new BridgewaterRibbon(this);
        }

        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support.
        /// </summary>
        private void InternalStartup()
        {
            Startup += ThisAddIn_Startup;
            Shutdown += ThisAddIn_Shutdown;
        }

        #endregion
    }
}
