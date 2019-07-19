// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;

    /// <summary>
    /// Simple id based key handle
    /// </summary>
    internal class KeyId : KeyHandle {

        /// <summary>
        /// Key handle
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Create key handle
        /// </summary>
        /// <param name="id"></param>
        public KeyId(string id) {
            Id = id;
        }

        /// <summary>
        /// Get id from handle
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public static string GetId(KeyHandle handle) {
            if (handle is KeyId id) {
                return id.Id;
            }
            throw new ArgumentException("Bad handle type");
        }
    }
}

