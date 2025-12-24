using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Bridgewater.DropboxLinker.Core.Contracts;

namespace Bridgewater.DropboxLinker.Outlook.Services
{
    /// <summary>
    /// Dialog shown when sending is blocked due to failed Dropbox link conversions.
    /// </summary>
    public sealed class SendBlockedDialog : Form
    {
        private readonly SendValidationResult _result;
        private readonly IDropboxAuthService _authService;
        private readonly IDropboxLinkService _linkService;
        private readonly IAppLogger _logger;

        private ListBox _failedList = null!;
        private Button _retryButton = null!;
        private Button _reauthButton = null!;
        private Button _removeButton = null!;
        private Button _cancelButton = null!;
        private Label _statusLabel = null!;

        /// <summary>
        /// Gets the selected recovery action.
        /// </summary>
        public RecoveryAction SelectedAction { get; private set; } = RecoveryAction.Cancel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendBlockedDialog"/> class.
        /// </summary>
        public SendBlockedDialog(
            SendValidationResult result,
            IDropboxAuthService authService,
            IDropboxLinkService linkService,
            IAppLogger logger)
        {
            _result = result ?? throw new ArgumentNullException(nameof(result));
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _linkService = linkService ?? throw new ArgumentNullException(nameof(linkService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponents();
            LoadFailedConversions();
        }

        private void InitializeComponents()
        {
            Text = "Cannot Send Email";
            Size = new Size(450, 350);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Header label
            var headerLabel = new Label
            {
                Text = "Some Dropbox links could not be created. " +
                       "Please resolve the issues below before sending.",
                Location = new Point(12, 12),
                Size = new Size(410, 40),
                Font = new Font(Font.FontFamily, 9f)
            };
            Controls.Add(headerLabel);

            // Failed items list
            _failedList = new ListBox
            {
                Location = new Point(12, 55),
                Size = new Size(410, 120),
                SelectionMode = SelectionMode.One
            };
            Controls.Add(_failedList);

            // Status label
            _statusLabel = new Label
            {
                Location = new Point(12, 180),
                Size = new Size(410, 20),
                ForeColor = Color.Gray,
                Text = ""
            };
            Controls.Add(_statusLabel);

            // Buttons panel
            var buttonY = 210;

            _retryButton = new Button
            {
                Text = "Retry",
                Location = new Point(12, buttonY),
                Size = new Size(95, 30)
            };
            _retryButton.Click += RetryButton_Click;
            Controls.Add(_retryButton);

            _reauthButton = new Button
            {
                Text = "Re-authenticate",
                Location = new Point(115, buttonY),
                Size = new Size(110, 30)
            };
            _reauthButton.Click += ReauthButton_Click;
            Controls.Add(_reauthButton);

            _removeButton = new Button
            {
                Text = "Remove Failed",
                Location = new Point(233, buttonY),
                Size = new Size(95, 30)
            };
            _removeButton.Click += RemoveButton_Click;
            Controls.Add(_removeButton);

            _cancelButton = new Button
            {
                Text = "Cancel Send",
                Location = new Point(336, buttonY),
                Size = new Size(90, 30),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(_cancelButton);

            CancelButton = _cancelButton;

            // Note about blocking
            var noteLabel = new Label
            {
                Text = "Note: Sending will remain blocked until all issues are resolved " +
                       "or failed items are removed.",
                Location = new Point(12, 250),
                Size = new Size(410, 40),
                ForeColor = Color.DarkRed,
                Font = new Font(Font.FontFamily, 8f)
            };
            Controls.Add(noteLabel);
        }

        private void LoadFailedConversions()
        {
            _failedList.Items.Clear();

            if (_result.FailedConversions != null)
            {
                foreach (var conversion in _result.FailedConversions)
                {
                    var displayText = $"✗ {conversion.FileName}";
                    if (!string.IsNullOrEmpty(conversion.ErrorMessage))
                    {
                        displayText += $" - {TruncateMessage(conversion.ErrorMessage, 50)}";
                    }
                    _failedList.Items.Add(new FailedItemWrapper(conversion, displayText));
                }
            }

            if (_failedList.Items.Count > 0)
            {
                _failedList.SelectedIndex = 0;
            }
        }

        private async void RetryButton_Click(object? sender, EventArgs e)
        {
            var selected = _failedList.SelectedItem as FailedItemWrapper;
            if (selected == null)
            {
                _statusLabel.Text = "Please select a failed item to retry.";
                return;
            }

            SetButtonsEnabled(false);
            _statusLabel.Text = $"Retrying {selected.Conversion.FileName}...";

            try
            {
                var request = new LinkRequest
                {
                    LocalFilePath = selected.Conversion.LocalPath,
                    FileSizeBytes = 0, // Will be updated
                    ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(7)
                };

                await _linkService.CreateOrReuseSharedLinkAsync(request, CancellationToken.None);
                
                _statusLabel.Text = $"Successfully created link for {selected.Conversion.FileName}";
                _statusLabel.ForeColor = Color.Green;

                // Remove from list
                _failedList.Items.Remove(selected);

                if (_failedList.Items.Count == 0)
                {
                    SelectedAction = RecoveryAction.Retry;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Retry failed for {selected.Conversion.FileName}");
                _statusLabel.Text = $"Retry failed: {TruncateMessage(ex.Message, 60)}";
                _statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async void ReauthButton_Click(object? sender, EventArgs e)
        {
            SetButtonsEnabled(false);
            _statusLabel.Text = "Opening Dropbox authentication...";

            try
            {
                await _authService.ReauthenticateAsync(CancellationToken.None);
                
                _statusLabel.Text = "Re-authentication successful. You can now retry failed items.";
                _statusLabel.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Re-authentication failed");
                _statusLabel.Text = $"Authentication failed: {TruncateMessage(ex.Message, 50)}";
                _statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            var selected = _failedList.SelectedItem as FailedItemWrapper;
            if (selected == null)
            {
                _statusLabel.Text = "Please select a failed item to remove.";
                return;
            }

            var confirm = MessageBox.Show(
                $"Remove the failed Dropbox link for '{selected.Conversion.FileName}'?\n\n" +
                "The link block will be removed from your email.",
                "Confirm Remove",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                _failedList.Items.Remove(selected);
                _statusLabel.Text = $"Removed {selected.Conversion.FileName}";

                if (_failedList.Items.Count == 0)
                {
                    SelectedAction = RecoveryAction.RemoveAll;
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            _retryButton.Enabled = enabled;
            _reauthButton.Enabled = enabled;
            _removeButton.Enabled = enabled;
        }

        private static string TruncateMessage(string message, int maxLength)
        {
            if (string.IsNullOrEmpty(message) || message.Length <= maxLength)
            {
                return message ?? "";
            }

            return message.Substring(0, maxLength - 3) + "...";
        }

        /// <summary>
        /// Wrapper for displaying failed items in the list.
        /// </summary>
        private sealed class FailedItemWrapper
        {
            public LinkConversionState Conversion { get; }
            private readonly string _displayText;

            public FailedItemWrapper(LinkConversionState conversion, string displayText)
            {
                Conversion = conversion;
                _displayText = displayText;
            }

            public override string ToString() => _displayText;
        }
    }

    /// <summary>
    /// Recovery action selected by the user.
    /// </summary>
    public enum RecoveryAction
    {
        /// <summary>User cancelled.</summary>
        Cancel,
        
        /// <summary>User successfully retried all failed items.</summary>
        Retry,
        
        /// <summary>User removed all failed items.</summary>
        RemoveAll
    }
}
