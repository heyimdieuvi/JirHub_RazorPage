using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JirHub.Services.ViNTD.IServices
{
    /// <summary>
    /// Service for encrypting and decrypting sensitive data using ASP.NET Core Data Protection.
    /// </summary>
    public interface IEncryptionService
    {
        /// <summary>
        /// Encrypts a plain text string.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <returns>The encrypted string, or null if input is null/empty.</returns>
        string Protect(string plainText);

        /// <summary>
        /// Decrypts an encrypted string.
        /// </summary>
        /// <param name="encryptedText">The encrypted text to decrypt.</param>
        /// <returns>The decrypted plain text, or null if input is null/empty.</returns>
        string Unprotect(string encryptedText);
    }
}
