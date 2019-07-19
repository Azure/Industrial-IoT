// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Certificate request object
    /// </summary>
    public class CertificationRequest {

        /// <summary>
        /// Subject name
        /// </summary>
        public X500DistinguishedName Subject { get; set; }

        /// <summary>
        /// Public key
        /// </summary>
        public Key PublicKey { get; set; }

        /// <summary>
        /// Extensions
        /// </summary>
        public List<X509Extension> Extensions { get; set; }

        /// <summary>
        /// Get raw asn1 data
        /// </summary>
        public byte[] RawData { get; set; }
    }
}

