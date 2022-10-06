// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol {
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Runtime;

    /// <summary>
    /// Security configuration
    /// </summary>
    public interface ISecurityConfig {

        /// <summary>
        /// PkiRootPath
        /// </summary>
        string PkiRootPath { get; }

        /// <summary>
        /// Certificate
        /// </summary>
        CertificateInfo ApplicationCertificate { get; }

        /// <summary>
        /// Whether to auto accept untrusted certificates
        /// </summary>
        bool AutoAcceptUntrustedCertificates { get; }

        /// <summary>
        /// Minimum key size
        /// </summary>
        ushort MinimumCertificateKeySize { get; }

        /// <summary>
        /// Rejected store
        /// </summary>
        CertificateStore RejectedCertificateStore { get; }

        /// <summary>
        /// Whether to reject unsecure signatures
        /// </summary>
        bool RejectSha1SignedCertificates { get; }

        /// <summary>
        /// Trusted certificates
        /// </summary>
        CertificateStore TrustedIssuerCertificates { get; }

        /// <summary>
        /// Trusted peer certificates
        /// </summary>
        CertificateStore TrustedPeerCertificates { get; }

        /// <summary>
        /// Automatically add application certificate to the trusted store
        /// </summary>
        bool AddAppCertToTrustedStore { get; }

        /// <summary>
        /// Reject chain validation with CA certs with unknown revocation status,
        /// e.g.when the CRL is not available or the OCSP provider is offline.
        /// </summary>
        bool RejectUnknownRevocationStatus { get; }
    }
}