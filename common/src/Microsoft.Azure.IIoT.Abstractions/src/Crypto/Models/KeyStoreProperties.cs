// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Crypto.Models {

    /// <summary>
    /// Key store parameters
    /// </summary>
    public class KeyStoreProperties {

        /// <summary>
        /// Whether the private key is exportable
        /// </summary>
        public bool Exportable { get; set; }
    }
}

