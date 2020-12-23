// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.KeyVault {

    /// <summary>
    /// Keyvault configuration
    /// </summary>
    public interface IKeyVaultConfig {

        /// <summary>
        /// Keyvault base url
        /// </summary>
        string KeyVaultBaseUrl { get; }

        /// <summary>
        /// Is hsm key vault
        /// </summary>
        bool KeyVaultIsHsm { get; }
    }
}