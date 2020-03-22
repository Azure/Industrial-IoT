// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System.Runtime.Serialization;
    using System;

    /// <summary>
    /// Crl document
    /// </summary>
    [DataContract]
    public class CrlDocument {

        /// <summary>
        /// Serial number of the certificate
        /// </summary>
        [DataMember(Name = "id")]
        public string CertificateSerialNumber { get; set; }

        /// <summary>
        /// Issuer serial number
        /// </summary>
        [DataMember]
        public string IssuerSerialNumber { get; set; }

        /// <summary>
        /// Crl serial number
        /// </summary>
        [DataMember]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Next update
        /// </summary>
        [DataMember]
        public DateTime? NextUpdate { get; set; }

        /// <summary>
        /// This update
        /// </summary>
        [DataMember]
        public DateTime ThisUpdate { get; set; }

        /// <summary>
        /// Raw crl for the certificate
        /// </summary>
        [DataMember]
        public byte[] RawData { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        [DataMember]
        public string Type => nameof(Crl);

        /// <summary>
        /// Expiration in seconds
        /// </summary>
        [DataMember(Name = "ttl")]
        public int Ttl { get; set; }
    }
}

