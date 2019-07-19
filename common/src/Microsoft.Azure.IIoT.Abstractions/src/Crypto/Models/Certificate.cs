// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// A Certificate
    /// </summary>
    public class Certificate {

        /// <summary>
        /// Key identifier to look up a private key
        /// </summary>
        public KeyHandle KeyHandle { get; set; }

        /// <summary>
        /// Issuer policies if certificate is issuer.
        /// </summary>
        public IssuerPolicies IssuerPolicies { get; set; }

        /// <summary>
        /// Revocation info if revoked
        /// </summary>
        public RevocationInfo Revoked { get; set; }

        /// <summary>
        /// Get raw asn1 data
        /// </summary>
        public byte[] RawData { get; set; }

        /// <summary>
        /// Not valid after in Utc
        /// </summary>
        public DateTime NotAfterUtc { get; set; }

        /// <summary>
        /// Not valid before in Utc
        /// </summary>
        public DateTime NotBeforeUtc { get; set; }

        /// <summary>
        /// Subject name
        /// </summary>
        public X500DistinguishedName Subject { get; set; }

        /// <summary>
        /// Issuer name
        /// </summary>
        public X500DistinguishedName Issuer { get; set; }

        /// <summary>
        /// Big endian Serial number
        /// </summary>
        public byte[] SerialNumber { get; set; }

        /// <summary>
        /// Big endian Issuer serial
        /// </summary>
        public byte[] IssuerSerialNumber { get; set; }

        /// <summary>
        /// Extensions
        /// </summary>
        public List<X509Extension> Extensions { get; set; }

        /// <summary>
        /// Get thumprint
        /// </summary>
        public string Thumbprint { get; set; }
    }
}

