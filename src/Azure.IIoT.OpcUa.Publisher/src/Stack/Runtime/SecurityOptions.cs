// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    /// <summary>
    /// Security configuration
    /// </summary>
    public sealed class SecurityOptions
    {
        /// <summary>
        /// PkiRootPath
        /// </summary>
        public string? PkiRootPath { get; set; }

        /// <summary>
        /// Application certificate store and subject
        /// </summary>
        public CertificateInfo? ApplicationCertificates { get; set; }

        /// <summary>
        /// Whether to auto accept untrusted certificates
        /// </summary>
        public bool? AutoAcceptUntrustedCertificates { get; set; }

        /// <summary>
        /// Minimum key size
        /// </summary>
        public ushort MinimumCertificateKeySize { get; set; }

        /// <summary>
        /// Rejected store
        /// </summary>
        public CertificateStore? RejectedCertificateStore { get; set; }

        /// <summary>
        /// Whether to reject unsecure signatures
        /// </summary>
        public bool? RejectSha1SignedCertificates { get; set; }

        /// <summary>
        /// Trusted certificates
        /// </summary>
        public CertificateStore? TrustedIssuerCertificates { get; set; }

        /// <summary>
        /// Trusted peer certificates
        /// </summary>
        public CertificateStore? TrustedPeerCertificates { get; set; }

        /// <summary>
        /// Automatically add application certificate to the trusted store
        /// </summary>
        public bool? AddAppCertToTrustedStore { get; set; }

        /// <summary>
        /// Reject chain validation with CA certs with unknown revocation status,
        /// e.g.when the CRL is not available or the OCSP provider is offline.
        /// </summary>
        public bool? RejectUnknownRevocationStatus { get; set; }

        /// <summary>
        /// Trusted user certificates
        /// </summary>
        public CertificateStore? TrustedUserCertificates { get; set; }

        /// <summary>
        /// Trusted https certificates
        /// </summary>
        public CertificateStore? TrustedHttpsCertificates { get; set; }

        /// <summary>
        /// Http issuer certificates (certificate authority)
        /// </summary>
        public CertificateStore? HttpsIssuerCertificates { get; set; }

        /// <summary>
        /// User issuer certificates
        /// </summary>
        public CertificateStore? UserIssuerCertificates { get; set; }

        /// <summary>
        /// Password to secure the key of the application certificate
        /// in the private key infrastructure.
        /// </summary>
        public string? ApplicationCertificatePassword { get; set; }

        /// <summary>
        /// Try to use the configuration from the first application
        /// certificate found in the application certificate store
        /// configured above.
        /// </summary>
        public bool? TryUseConfigurationFromExistingAppCert { get; set; }
    }
}
