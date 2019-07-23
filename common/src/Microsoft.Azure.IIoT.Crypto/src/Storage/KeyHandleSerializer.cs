// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Microsoft.Azure.IIoT.Crypto.Storage.Models;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Key document key handle serializer
    /// </summary>
    public class KeyHandleSerializer : IKeyHandleSerializer {

        /// <inheritdoc/>
        public JToken SerializeHandle(KeyHandle handle) {
            if (handle is KeyId id) {
                return JToken.FromObject(id);
            }
            throw new ArgumentException("Bad handle type");
        }

        /// <inheritdoc/>
        public KeyHandle DeserializeHandle(JToken token) {
            return token.ToObject<KeyId>();
        }
    }
}

