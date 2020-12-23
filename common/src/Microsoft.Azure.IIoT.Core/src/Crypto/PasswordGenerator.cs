// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Default {
    using Microsoft.Azure.IIoT.Auth;
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Generate password
    /// </summary>
    public class PasswordGenerator : IPasswordGenerator {

        /// <inheritdoc/>
        public async Task<string> GeneratePassword(int length, AllowedChars allowedChars, bool asBase64) {
            var allowed = string.Empty;

            if (allowedChars == AllowedChars.All) {
                allowed = kUppercaseLetters + kLowercaseLetters + kDigits + kSpecial;
            }
            else {
                if ((allowedChars & AllowedChars.Uppercase) == AllowedChars.Uppercase) {
                    allowed += kUppercaseLetters;
                }

                if ((allowedChars & AllowedChars.Lowercase) == AllowedChars.Lowercase) {
                    allowed += kLowercaseLetters;
                }

                if ((allowedChars & AllowedChars.Digits) == AllowedChars.Digits) {
                    allowed += kDigits;
                }

                if ((allowedChars & AllowedChars.Special) == AllowedChars.Special) {
                    allowed += kSpecial;
                }
            }

            return await GeneratePassword(length, allowed, asBase64);
        }

        /// <summary>
        /// Generate password
        /// </summary>
        /// <param name="length"></param>
        /// <param name="allowedChars"></param>
        /// <param name="asBase64"></param>
        /// <returns></returns>
        private Task<string> GeneratePassword(int length, string allowedChars, bool asBase64) {
            var pw = new byte[length];

            using (var crypto = new RNGCryptoServiceProvider()) {
                crypto.GetBytes(pw);
            }

            var result = new StringBuilder(length);

            foreach (var c in pw) {
                result.Append(allowedChars[c % allowedChars.Length]);
            }

            var pResult = result.ToString();

            if (asBase64) {
                var plainBytes = Encoding.UTF8.GetBytes(pResult);
                pResult = Convert.ToBase64String(plainBytes);
            }

            return Task.FromResult(pResult);
        }

        private const string kUppercaseLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string kLowercaseLetters = "abcdefghijklmnopqrstuvwxyz";
        private const string kDigits = "0123456789";
        private const string kSpecial = "!§$%&/()=?-_.,:;";
    }
}