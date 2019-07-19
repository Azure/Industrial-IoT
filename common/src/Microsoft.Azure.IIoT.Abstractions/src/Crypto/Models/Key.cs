// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Crypto.Models {

    /// <summary>
    /// Public and/or private key
    /// </summary>
    public class Key {

        /// <summary>
        /// Key Type
        /// </summary>
        public KeyType Type { get; set; }

        /// <summary>
        /// Key Parameters
        /// </summary>
        public KeyParams Parameters { get; set; }
    }
}

