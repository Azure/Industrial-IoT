// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Crypto {
    using Microsoft.Azure.IIoT.Crypto.Models;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Issues certificates
    /// </summary>
    public interface ICertificateIssuer {

        /// <summary>
        /// Imports an existing certificate with private key
        /// If not self signed, the issuer certificate must
        /// already be present.
        /// </summary>
        /// <param name="certificateName"></param>
        /// <param name="certificate"></param>
        /// <param name="privateKey"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Certificate> ImportCertificateAsync(string certificateName,
            Certificate certificate, Key privateKey = null,
            CancellationToken ct = default);

        /// <summary>
        /// Creates a new self signed root certificate with specified
        /// name to issue new certificates with.
        /// </summary>
        /// <param name="certificateName"></param>
        /// <param name="subjectName"></param>
        /// <param name="notBefore"></param>
        /// <param name="lifetime"></param>
        /// <param name="keyParams"></param>
        /// <param name="policies"></param>
        /// <param name="extensions"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Certificate> NewRootCertificateAsync(string certificateName,
            X500DistinguishedName subjectName, DateTime? notBefore,
            TimeSpan lifetime, CreateKeyParams keyParams,
            IssuerPolicies policies,
            Func<byte[], IEnumerable<X509Extension>> extensions = null,
            CancellationToken ct = default);

        /// <summary>
        /// Issues a new signed certificate with specified name to
        /// issue new certificates with.
        /// </summary>
        /// <param name="issuerCertificate"></param>
        /// <param name="certificateName"></param>
        /// <param name="subjectName"></param>
        /// <param name="notBefore"></param>
        /// <param name="keyParams"></param>
        /// <param name="policies"></param>
        /// <param name="extensions"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Certificate> NewIssuerCertificateAsync(string issuerCertificate,
            string certificateName, X500DistinguishedName subjectName,
            DateTime? notBefore, CreateKeyParams keyParams, IssuerPolicies policies,
            Func<byte[], IEnumerable<X509Extension>> extensions = null,
            CancellationToken ct = default);

        /// <summary>
        /// Create new certificate with provided public key
        /// using the provided certificate factory.
        /// </summary>
        /// <param name="issuerCertificate"></param>
        /// <param name="certificateName"></param>
        /// <param name="publicKey"></param>
        /// <param name="subjectName"></param>
        /// <param name="notBefore"></param>
        /// <param name="extensions"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Certificate> CreateSignedCertificateAsync(
            string issuerCertificate, string certificateName, Key publicKey,
            X500DistinguishedName subjectName, DateTime? notBefore,
            Func<byte[], IEnumerable<X509Extension>> extensions = null,
            CancellationToken ct = default);

        /// <summary>
        /// Creates key and new certificate with specified
        /// name using the provided certificate factory.
        /// </summary>
        /// <param name="issuerCertificate"></param>
        /// <param name="certificateName"></param>
        /// <param name="subjectName"></param>
        /// <param name="notBefore"></param>
        /// <param name="keyParams"></param>
        /// <param name="extensions"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<Certificate> CreateCertificateAndPrivateKeyAsync(
            string issuerCertificate, string certificateName,
            X500DistinguishedName subjectName, DateTime? notBefore,
            CreateKeyParams keyParams,
            Func<byte[], IEnumerable<X509Extension>> extensions = null,
            CancellationToken ct = default);

        /// <summary>
        /// Disable single issued certificate
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DisableCertificateAsync(Certificate certificate,
            CancellationToken ct = default);
    }
}