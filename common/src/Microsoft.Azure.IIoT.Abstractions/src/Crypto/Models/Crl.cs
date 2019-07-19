// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;

    /// <summary>
    /// Represents a crl in lieu of .net having one.
    /// </summary>
    public sealed class Crl {

        /// <summary>
        /// Crl number
        /// </summary>
        public byte[] SerialNumber { get; set; }

        /// <summary>
        /// The subject name of the Issuer for the CRL.
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// When this CRL was updated.
        /// </summary>
        public DateTime ThisUpdate { get; set; }

        /// <summary>
        /// When the CRL is due for its next update.
        /// </summary>
        public DateTime? NextUpdate { get; set; }

        /// <summary>
        /// The raw asn.1 data for the CRL.
        /// </summary>
        public byte[] RawData { get; set; }
    }
}
