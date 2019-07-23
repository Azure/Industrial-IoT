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
    /// Certificate factory
    /// </summary>
    public interface ICertificateFactory {

        /// <summary>
        /// Creates a signed certificate
        /// </summary>
        /// <param name="signer"></param>
        /// <param name="issuer">Issuer certificate</param>
        /// <param name="subjectName"></param>
        /// <param name="publicKey">Public key</param>
        /// <param name="notBefore"></param>
        /// <param name="notAfter"></param>
        /// <param name="signatureType"></param>
        /// <param name="canIssue"></param>
        /// <param name="extensions"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509Certificate2> CreateCertificateAsync(IDigestSigner signer,
            Certificate issuer, X500DistinguishedName subjectName, Key publicKey,
            DateTime notBefore, DateTime notAfter, SignatureType signatureType,
            bool canIssue, Func<byte[], IEnumerable<X509Extension>> extensions = null,
            CancellationToken ct = default);

        /// <summary>
        /// Creates a self signed certificate
        /// </summary>
        /// <param name="signer"></param>
        /// <param name="signingKey">private key</param>
        /// <param name="subjectName"></param>
        /// <param name="publicKey">Public key</param>
        /// <param name="notBefore"></param>
        /// <param name="notAfter"></param>
        /// <param name="signatureType"></param>
        /// <param name="canIssue"></param>
        /// <param name="extensions"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<X509Certificate2> CreateCertificateAsync(IDigestSigner signer,
            KeyHandle signingKey, X500DistinguishedName subjectName, Key publicKey,
            DateTime notBefore, DateTime notAfter, SignatureType signatureType,
            bool canIssue, Func<byte[], IEnumerable<X509Extension>> extensions = null,
            CancellationToken ct = default);
    }
}