// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Crypto.Models {

    /// <summary>
    /// Aes params
    /// </summary>
    public class AesParams : KeyParams {

        /// <summary>
        /// Symmetric key
        /// </summary>
        public byte[] K { get; set; }
    }
}