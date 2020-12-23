// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Auth {
    using System.Threading.Tasks;

    /// <summary>
    /// Passord generator
    /// </summary>
    public interface IPasswordGenerator {

        /// <summary>
        /// Generate password
        /// </summary>
        /// <param name="length"></param>
        /// <param name="allowedChars"></param>
        /// <param name="asBase64"></param>
        /// <returns></returns>
        Task<string> GeneratePassword(int length, AllowedChars allowedChars, bool asBase64);
    }
}