using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JirHub.Services.ViNTD.IServices;
using Microsoft.AspNetCore.DataProtection;

namespace JirHub.Services.ViNTD.Services
{
    /// <summary>
    /// Implementation of IEncryptionService using IDataProtectionProvider.
    /// Ensures tokens are encrypted at rest and never logged or exposed in plain text.
    /// </summary>
    public class EncryptionService : IEncryptionService
    {
        private readonly IDataProtector _protector;

        public EncryptionService(IDataProtectionProvider dataProtectionProvider)
        {
            // Create a purpose-specific protector for API tokens
            _protector = dataProtectionProvider.CreateProtector("JirHub.Member2.API_Tokens");
        }

        /// <summary>
        /// Encrypts a plain text string using Data Protection API.
        /// </summary>
        /// <param name="plainText">The plain text to encrypt.</param>
        /// <returns>The encrypted string, or null if input is null/empty.</returns>
        public string Protect(string plainText)
        {
            if (string.IsNullOrWhiteSpace(plainText))
                return null;

            return _protector.Protect(plainText);
        }

        /// <summary>
        /// Decrypts an encrypted string using Data Protection API.
        /// </summary>
        /// <param name="encryptedText">The encrypted text to decrypt.</param>
        /// <returns>The decrypted plain text, or null if input is null/empty.</returns>
        public string Unprotect(string encryptedText)
        {
            if (string.IsNullOrWhiteSpace(encryptedText))
                return null;

            try
            {
                return _protector.Unprotect(encryptedText);
            }
            catch (Exception)
            {
                // If decryption fails (corrupted data, wrong key, etc.), return null
                // Never log the encrypted text to prevent token exposure
                return null;
            }
        }
    }
}
