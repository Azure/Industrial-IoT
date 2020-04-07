// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Storage.Models {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Certificate document
    /// </summary>
    [DataContract]
    public class CertificateDocument {

        /// <summary>
        /// Serial number
        /// </summary>
        [DataMember(Name = "id")]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Thumbprint
        /// </summary>
        [DataMember]
        public string Thumbprint { get; set; }

        /// <summary>
        /// Internal Certificate identifier
        /// </summary>
        [DataMember]
        public string CertificateId { get; set; }

        /// <summary>
        /// Internal Certificate name
        /// </summary>
        [DataMember]
        public string CertificateName { get; set; }

        /// <summary>
        /// Certificate subject
        /// </summary>
        [DataMember]
        public string Subject { get; set; }

        /// <summary>
        /// Subject key identifier
        /// </summary>
        [DataMember]
        public string KeyId { get; set; }

        /// <summary>
        /// Alternative names of the subject
        /// </summary>
        [DataMember]
        public List<string> SubjectAltNames { get; set; }

        /// <summary>
        /// Certificate issuer
        /// </summary>
        [DataMember]
        public string Issuer { get; set; }

        /// <summary>
        /// Alternative names of the issuer
        /// </summary>
        [DataMember]
        public List<string> IssuerAltNames { get; set; }

        /// <summary>
        /// Issuer key id
        /// </summary>
        [DataMember]
        public string IssuerKeyId { get; set; }

        /// <summary>
        /// Issuer serial number
        /// </summary>
        [DataMember]
        public string IssuerSerialNumber { get; set; }

        /// <summary>
        /// Issuer policies
        /// </summary>
        [DataMember]
        public IssuerPolicies IsserPolicies { get; set; }

        /// <summary>
        /// When the certificate was disabled
        /// </summary>
        [DataMember]
        public DateTime? DisabledSince { get; set; }

        /// <summary>
        /// Certificate is an issuer certificate
        /// </summary>
        [DataMember]
        public bool IsIssuer { get; set; }

        /// <summary>
        /// Valid not before
        /// </summary>
        [DataMember]
        public DateTime NotBefore { get; set; }

        /// <summary>
        /// Valid not after
        /// </summary>
        [DataMember]
        public DateTime NotAfter { get; set; }

        /// <summary>
        /// Private key identifier
        /// </summary>
        [DataMember]
        public byte[] KeyHandle { get; set; }

        /// <summary>
        /// Raw certificate
        /// </summary>
        [DataMember]
        public byte[] RawData { get; set; }

        /// <summary>
        /// Certificate version
        /// </summary>
        [DataMember]
        public long Version { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        [DataMember]
        public string Type => nameof(Certificate);
    }
}

