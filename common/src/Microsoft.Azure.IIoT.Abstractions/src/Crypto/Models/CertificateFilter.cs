// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto.Models {
    using System;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// A Certificate query filter
    /// </summary>
    public sealed class CertificateFilter {

        /// <summary>
        /// With specific name
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// Certificates not valid before
        /// </summary>
        public DateTime? NotBefore { get; set; }

        /// <summary>
        /// Certificates not valid after
        /// </summary>
        public DateTime? NotAfter { get; set; }

        /// <summary>
        /// Include Alternative names in search
        /// </summary>
        public bool IncludeAltNames { get; set; }

        /// <summary>
        /// With specific subject
        /// </summary>
        public X500DistinguishedName Subject { get; set; }

        /// <summary>
        /// With specific subject identifier
        /// e.g. application id or endpoint id
        /// </summary>
        public string SubjectId { get; set; }

        /// <summary>
        /// With specific thumbprint
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        /// Subject key id
        /// </summary>
        public string KeyId { get; set; }

        /// <summary>
        /// With specific issuer
        /// </summary>
        public X500DistinguishedName Issuer { get; set; }

        /// <summary>
        /// With specific issuer serial number
        /// </summary>
        public byte[] IssuerSerialNumber { get; set; }

        /// <summary>
        /// Issuer key id
        /// </summary>
        public string IssuerKeyId { get; set; }

        /// <summary>
        /// Whether to return disabled
        /// </summary>
        public bool IncludeDisabled { get; set; }

        /// <summary>
        /// Whether to exclude enabled
        /// </summary>
        public bool ExcludeEnabled { get; set; }

        /// <summary>
        /// Include only issuers - if false,
        /// include only leaf entries, if null
        /// include all.
        /// </summary>
        public bool? IsIssuer { get; set; }
    }
}

