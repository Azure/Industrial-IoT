// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {

    /// <summary>
    /// Ecc parameters
    /// </summary>
    public class EccParams : KeyParams {

        /// <summary>
        /// Name of the curve
        /// </summary>
        public CurveType Curve { get; set; }

        /// <summary>
        /// Represents the private key D
        /// stored in big-endian format.
        /// </summary>
        public byte[] D { get; set; }

        /// <summary>
        /// Represents the public key X-Coord.
        /// </summary>
        public byte[] X { get; set; }

        /// <summary>
        /// Represents the public key Y-Coord.
        /// </summary>
        public byte[] Y { get; set; }
    }
}