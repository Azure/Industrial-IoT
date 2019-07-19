// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.KeyVault.Clients {
    using Microsoft.Azure.IIoT.Crypto.KeyVault.Models;
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Keyvault key handle serializer
    /// </summary>
    public class KeyVaultKeyHandleSerializer : IKeyHandleSerializer {

        /// <inheritdoc/>
        public JToken SerializeHandle(KeyHandle handle) {
            if (handle is KeyVaultKeyHandle id) {
                return JToken.FromObject(id);
            }
            throw new ArgumentException("Bad handle type");
        }

        /// <inheritdoc/>
        public KeyHandle DeserializeHandle(JToken token) {
            return token.ToObject<KeyVaultKeyHandle>();
        }
    }
}