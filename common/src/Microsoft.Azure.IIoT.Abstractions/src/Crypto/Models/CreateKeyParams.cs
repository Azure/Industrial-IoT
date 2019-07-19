// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Crypto.Models {

    /// <summary>
    /// Create key parameters
    /// </summary>
    public class CreateKeyParams {

        /// <summary>
        /// Type of key
        /// </summary>
        public KeyType Type { get; set; }

        /// <summary>
        /// Gets or sets the desired key size.
        /// </summary>
        public uint? KeySize { get; set; }

        /// <summary>
        /// Gets or sets the desired curve type
        /// for used with ECC algorithms.
        /// </summary>
        public CurveType? Curve { get; set; }
    }
}

