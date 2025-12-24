using System.Collections.Generic;
using System.Linq;

namespace Bridgewater.DropboxLinker.Outlook.Services
{
    /// <summary>
    /// Tracks the state of Dropbox link conversions for each email message.
    /// </summary>
    public sealed class LinkConversionTracker
    {
        private readonly Dictionary<string, List<LinkConversionState>> _conversions = 
            new Dictionary<string, List<LinkConversionState>>();
        private readonly object _lock = new object();

        /// <summary>
        /// Adds a conversion to track for a specific email.
        /// </summary>
        /// <param name="emailId">The email entry ID or unique identifier.</param>
        /// <param name="conversion">The conversion state to track.</param>
        public void AddConversion(string emailId, LinkConversionState conversion)
        {
            lock (_lock)
            {
                if (!_conversions.TryGetValue(emailId, out var list))
                {
                    list = new List<LinkConversionState>();
                    _conversions[emailId] = list;
                }

                list.Add(conversion);
            }
        }

        /// <summary>
        /// Gets all conversions for a specific email.
        /// </summary>
        /// <param name="emailId">The email entry ID or unique identifier.</param>
        /// <returns>A read-only list of conversion states, or null if none.</returns>
        public IReadOnlyList<LinkConversionState>? GetConversions(string emailId)
        {
            lock (_lock)
            {
                if (_conversions.TryGetValue(emailId, out var list))
                {
                    return list.ToList().AsReadOnly();
                }

                return null;
            }
        }

        /// <summary>
        /// Gets all failed conversions for a specific email.
        /// </summary>
        /// <param name="emailId">The email entry ID or unique identifier.</param>
        /// <returns>A list of failed conversion states.</returns>
        public IReadOnlyList<LinkConversionState> GetFailedConversions(string emailId)
        {
            lock (_lock)
            {
                if (_conversions.TryGetValue(emailId, out var list))
                {
                    return list.Where(c => c.Status == ConversionStatus.Failed).ToList().AsReadOnly();
                }

                return new List<LinkConversionState>().AsReadOnly();
            }
        }

        /// <summary>
        /// Updates a conversion's status.
        /// </summary>
        /// <param name="emailId">The email entry ID.</param>
        /// <param name="localPath">The local file path to identify the conversion.</param>
        /// <param name="status">The new status.</param>
        /// <param name="resultUrl">The result URL if successful.</param>
        /// <param name="errorMessage">The error message if failed.</param>
        public void UpdateConversion(
            string emailId, 
            string localPath, 
            ConversionStatus status,
            string? resultUrl = null,
            string? errorMessage = null)
        {
            lock (_lock)
            {
                if (_conversions.TryGetValue(emailId, out var list))
                {
                    var conversion = list.FirstOrDefault(c => c.LocalPath == localPath);
                    if (conversion != null)
                    {
                        conversion.Status = status;
                        conversion.ResultUrl = resultUrl;
                        conversion.ErrorMessage = errorMessage;
                    }
                }
            }
        }

        /// <summary>
        /// Removes a specific conversion from tracking.
        /// </summary>
        /// <param name="emailId">The email entry ID.</param>
        /// <param name="localPath">The local file path to identify the conversion.</param>
        public void RemoveConversion(string emailId, string localPath)
        {
            lock (_lock)
            {
                if (_conversions.TryGetValue(emailId, out var list))
                {
                    list.RemoveAll(c => c.LocalPath == localPath);
                    
                    if (list.Count == 0)
                    {
                        _conversions.Remove(emailId);
                    }
                }
            }
        }

        /// <summary>
        /// Clears all conversions for a specific email.
        /// </summary>
        /// <param name="emailId">The email entry ID.</param>
        public void ClearConversions(string emailId)
        {
            lock (_lock)
            {
                _conversions.Remove(emailId);
            }
        }

        /// <summary>
        /// Checks if there are any failed conversions for a specific email.
        /// </summary>
        /// <param name="emailId">The email entry ID.</param>
        /// <returns><c>true</c> if there are failed conversions; otherwise, <c>false</c>.</returns>
        public bool HasFailedConversions(string emailId)
        {
            lock (_lock)
            {
                if (_conversions.TryGetValue(emailId, out var list))
                {
                    return list.Any(c => c.Status == ConversionStatus.Failed);
                }

                return false;
            }
        }

        /// <summary>
        /// Checks if there are any pending or in-progress conversions for a specific email.
        /// </summary>
        /// <param name="emailId">The email entry ID.</param>
        /// <returns><c>true</c> if there are pending conversions; otherwise, <c>false</c>.</returns>
        public bool HasPendingConversions(string emailId)
        {
            lock (_lock)
            {
                if (_conversions.TryGetValue(emailId, out var list))
                {
                    return list.Any(c => 
                        c.Status == ConversionStatus.Pending || 
                        c.Status == ConversionStatus.InProgress);
                }

                return false;
            }
        }
    }
}
