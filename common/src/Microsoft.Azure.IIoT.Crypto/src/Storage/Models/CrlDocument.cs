// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Crl document
    /// </summary>
    public class CrlDocument {

        /// <summary>
        /// Serial number of the certificate
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string CertificateSerialNumber { get; set; }

        /// <summary>
        /// Issuer serial number
        /// </summary>
        public string IssuerSerialNumber { get; set; }

        /// <summary>
        /// Crl serial number
        /// </summary>
        public string SerialNumber { get; set; }

        /// <summary>
        /// Next update
        /// </summary>
        public DateTime? NextUpdate { get; set; }

        /// <summary>
        /// This update
        /// </summary>
        public DateTime ThisUpdate { get; set; }

        /// <summary>
        /// Raw crl for the certificate
        /// </summary>
        public byte[] RawData { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public string Type => nameof(Crl);

        /// <summary>
        /// Expiration in seconds
        /// </summary>
        [JsonProperty(PropertyName = "ttl")]
        public int Ttl { get; set; }
    }
}

