// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.KeyVault.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.KeyVault.Models;
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Key and secret identifier keyvault bundle handle
    /// </summary>
    [DataContract]
    internal class KeyVaultKeyHandle : KeyHandle {

        /// <summary>
        /// Key identifier
        /// </summary>
        [DataMember]
        public string SecretIdentifier { get; internal set; }

        /// <summary>
        /// Key identifier
        /// </summary>
        [DataMember]
        public string KeyIdentifier { get; internal set; }


        /// <summary>
        /// Create key handle
        /// </summary>
        /// <param name="keyIdentifier"></param>
        /// <param name="secretIdentifier"></param>
        internal static KeyVaultKeyHandle Create(string keyIdentifier,
            string secretIdentifier) {
            return new KeyVaultKeyHandle {
                KeyIdentifier = keyIdentifier,
                SecretIdentifier = secretIdentifier
            };
        }

        /// <summary>
        /// Create key handle
        /// </summary>
        /// <param name="bundle"></param>
        internal static KeyVaultKeyHandle Create(CertificateBundle bundle) {
            return Create(bundle.KeyIdentifier?.Identifier,
                bundle.SecretIdentifier?.Identifier);
        }

        /// <summary>
        /// Create key handle
        /// </summary>
        /// <param name="bundle"></param>
        /// <param name="secret"></param>
        internal static KeyVaultKeyHandle Create(KeyBundle bundle,
            SecretBundle secret = null) {
            return Create(bundle.KeyIdentifier?.Identifier,
                secret.SecretIdentifier?.Identifier);
        }

        /// <summary>
        /// Get id from handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static KeyVaultKeyHandle GetBundle(KeyHandle handle) {
            if (handle is KeyVaultKeyHandle id) {
                return id;
            }
            throw new ArgumentException("Bad handle type");
        }
    }
}