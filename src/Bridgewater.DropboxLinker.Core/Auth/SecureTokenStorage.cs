using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Bridgewater.DropboxLinker.Core.Auth
{
    /// <summary>
    /// Securely stores OAuth tokens using Windows Credential Manager.
    /// </summary>
    public sealed class SecureTokenStorage
    {
        private const string CredentialTarget = "BridgewaterDropboxLinker";

        /// <summary>
        /// Stores the refresh token securely.
        /// </summary>
        /// <param name="refreshToken">The refresh token to store.</param>
        /// <returns><c>true</c> if stored successfully; otherwise, <c>false</c>.</returns>
        public bool StoreRefreshToken(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                return false;
            }

            try
            {
                var tokenBytes = Encoding.UTF8.GetBytes(refreshToken);

                var credential = new CREDENTIAL
                {
                    Type = CRED_TYPE_GENERIC,
                    TargetName = CredentialTarget,
                    CredentialBlobSize = (uint)tokenBytes.Length,
                    CredentialBlob = Marshal.AllocHGlobal(tokenBytes.Length),
                    Persist = CRED_PERSIST_LOCAL_MACHINE,
                    UserName = "DropboxRefreshToken"
                };

                Marshal.Copy(tokenBytes, 0, credential.CredentialBlob, tokenBytes.Length);

                try
                {
                    return CredWrite(ref credential, 0);
                }
                finally
                {
                    Marshal.FreeHGlobal(credential.CredentialBlob);
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieves the stored refresh token.
        /// </summary>
        /// <returns>The refresh token, or <c>null</c> if not found.</returns>
        public string? GetRefreshToken()
        {
            try
            {
                if (!CredRead(CredentialTarget, CRED_TYPE_GENERIC, 0, out var credentialPtr))
                {
                    return null;
                }

                try
                {
                    var credential = Marshal.PtrToStructure<CREDENTIAL>(credentialPtr);
                    if (credential.CredentialBlobSize > 0 && credential.CredentialBlob != IntPtr.Zero)
                    {
                        var tokenBytes = new byte[credential.CredentialBlobSize];
                        Marshal.Copy(credential.CredentialBlob, tokenBytes, 0, (int)credential.CredentialBlobSize);
                        return Encoding.UTF8.GetString(tokenBytes);
                    }

                    return null;
                }
                finally
                {
                    CredFree(credentialPtr);
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deletes the stored refresh token.
        /// </summary>
        /// <returns><c>true</c> if deleted successfully; otherwise, <c>false</c>.</returns>
        public bool DeleteRefreshToken()
        {
            try
            {
                return CredDelete(CredentialTarget, CRED_TYPE_GENERIC, 0);
            }
            catch
            {
                return false;
            }
        }

        #region Windows Credential Manager P/Invoke

        private const int CRED_TYPE_GENERIC = 1;
        private const int CRED_PERSIST_LOCAL_MACHINE = 2;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIAL
        {
            public int Flags;
            public int Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? Comment;
            public long LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public int Persist;
            public int AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? UserName;
        }

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredWrite(ref CREDENTIAL credential, int flags);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredRead(string target, int type, int flags, out IntPtr credential);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool CredFree(IntPtr credential);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CredDelete(string target, int type, int flags);

        #endregion
    }
}
