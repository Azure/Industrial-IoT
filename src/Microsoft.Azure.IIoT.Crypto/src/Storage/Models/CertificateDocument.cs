// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Certificate document
    /// </summary>
    public class CertificateDocument {

        /// <summary>
        /// Serial number
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Thumbprint
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        /// Internal Certificate identifier
        /// </summary>
        public string CertificateId { get; set; }

        /// <summary>
        /// Internal Certificate name
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// Certificate subject
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Subject key identifier
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// Alternative names of the subject
        /// </summary>
        public List<string> SubjectAltNames { get; set; }

        /// <summary>
        /// Certificate issuer
        /// </summary>
        public string Issuer { get; set; }

        /// <summary>
        /// Alternative names of the issuer
        /// </summary>
        public List<string> IssuerAltNames { get; set; }

        /// <summary>
        /// Issuer key id
        /// </summary>
        public string IssuerKeyId { get; set; }

        /// <summary>
        /// Issuer serial number
        /// </summary>
        public string IssuerSerialNumber { get; set; }

        /// <summary>
        /// Issuer policies
        /// </summary>
        public IssuerPolicies IsserPolicies { get; set; }

        /// <summary>
        /// When the certificate was disabled
        /// </summary>
        public DateTime? DisabledSince { get; set; }

        /// <summary>
        /// Certificate is an issuer certificate
        /// </summary>
        public bool IsIssuer { get; set; }

        /// <summary>
        /// Valid not before
        /// </summary>
        public DateTime NotBefore { get; set; }

        /// <summary>
        /// Valid not after
        /// </summary>
        public DateTime NotAfter { get; set; }

        /// <summary>
        /// Private key identifier
        /// </summary>
        public JToken KeyHandle { get; set; }

        /// <summary>
        /// Raw certificate
        /// </summary>
        public byte[] RawData { get; set; }

        /// <summary>
        /// Certificate version
        /// </summary>
        public long Version { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        public string Type => nameof(Certificate);
    }
}

