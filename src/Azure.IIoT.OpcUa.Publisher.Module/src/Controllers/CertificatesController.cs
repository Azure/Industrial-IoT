// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// This section lists the certificate APi provided by OPC Publisher providing
    /// all public and private key infrastructure (PKI) related API methods.
    /// </para>
    /// <para>
    /// The method name for all transports other than HTTP (which uses the shown
    /// HTTP methods and resource uris) is the name of the subsection header.
    /// To use the version specific method append "_V1" or "_V2" to the method
    /// name.
    /// </para>
    /// </summary>
    [Version("_V1")]
    [Version("_V2")]
    [Version("")]
    [RouterExceptionFilter]
    [ControllerExceptionFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/pki")]
    [ApiController]
    public class CertificatesController : ControllerBase, IMethodController
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="certificates"></param>
        public CertificatesController(IOpcUaCertificates certificates)
        {
            _certificates = certificates ??
                throw new ArgumentNullException(nameof(certificates));
        }

        /// <summary>
        /// ListCertificates
        /// </summary>
        /// <remarks>
        /// Get the certificates in the specified certificate store
        /// </remarks>
        /// <param name="store">The store to enumerate</param>
        /// <param name="ct"></param>
        /// <returns>The list of certificates currently in the store.</returns>
        [HttpGet("{store}/certs")]
        public async Task<IReadOnlyList<X509CertificateModel>> ListCertificatesAsync(
            CertificateStoreName store, CancellationToken ct = default)
        {
            return await _certificates.ListCertificatesAsync(store, false,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// ListCertificateRevocationLists
        /// </summary>
        /// <remarks>
        /// Get the certificates in the specified certificated store
        /// </remarks>
        /// <param name="store">The store to enumerate</param>
        /// <param name="ct"></param>
        /// <returns>The list of certificates revocation lists currently
        /// in the store.</returns>
        [HttpGet("{store}/crls")]
        public async Task<IReadOnlyList<byte[]>> ListCertificateRevocationListsAsync(
            CertificateStoreName store, CancellationToken ct = default)
        {
            return await _certificates.ListCertificateRevocationListsAsync(store,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// AddCertificate
        /// </summary>
        /// <remarks>
        /// Add a certificate to the specified store. The certificate is provided
        /// as a pfx/pkcs12 optionally password protected blob.
        /// </remarks>
        /// <param name="store">The store to add the certificate to</param>
        /// <param name="pfxBlob">The pfx encoded certificate.</param>
        /// <param name="password">The optional password of the pfx</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="pfxBlob"/>
        /// is <c>null</c>.</exception>
        [HttpPatch("{store}/certs")]
        public async Task AddCertificateAsync(CertificateStoreName store,
            [FromBody][Required] byte[] pfxBlob, [FromQuery] string? password,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pfxBlob);
            await _certificates.AddCertificateAsync(store, pfxBlob, password,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// AddCertificateRevocationList
        /// </summary>
        /// <remarks>
        /// Add a certificate revocation list to the specified store. The certificate
        /// revocation list is provided as a der encoded blob.
        /// </remarks>
        /// <param name="store">The store to add the certificate to</param>
        /// <param name="crl">The pfx encoded certificate.</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="crl"/>
        /// is <c>null</c>.</exception>
        [HttpPatch("{store}/crls")]
        public async Task AddCertificateRevocationListAsync(CertificateStoreName store,
            [FromBody][Required] byte[] crl, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(crl);
            await _certificates.AddCertificateRevocationListAsync(store, crl,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// AddCertificateChain
        /// </summary>
        /// <remarks>
        /// Add a certificate chain to the specified store. The certificate is provided
        /// as a concatenated asn encoded set of certificates with the first the
        /// one to add, and the remainder the issuer chain.
        /// </remarks>
        /// <param name="certificateChain">The certificate chain.</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="certificateChain"/>
        /// is <c>null</c>.</exception>
        [HttpPost("trusted/certs")]
        public async Task AddCertificateChainAsync([FromBody][Required] byte[] certificateChain,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(certificateChain);
            await _certificates.AddCertificateChainAsync(certificateChain, false,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// ApproveRejectedCertificate
        /// </summary>
        /// <remarks>
        /// Move a rejected certificate from the rejected folder to the trusted
        /// folder on the publisher.
        /// </remarks>
        /// <param name="thumbprint">The thumbprint of the certificate to trust.</param>
        /// <param name="ct"></param>
        [HttpPost("rejected/certs/{thumbprint}/approve")]
        public async Task ApproveRejectedCertificateAsync(string thumbprint,
            CancellationToken ct = default)
        {
            await _certificates.ApproveRejectedCertificateAsync(thumbprint,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// AddTrustedHttpsCertificateAsync
        /// </summary>
        /// <remarks>
        /// Add a certificate chain to the trusted https store. The certificate is
        /// provided as a concatenated set of certificates with the first the
        /// one to add, and the remainder the issuer chain.
        /// </remarks>
        /// <param name="certificateChain">The certificate chain.</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="certificateChain"/>
        /// is <c>null</c>.</exception>
        [HttpPost("https/certs")]
        public async Task AddTrustedHttpsCertificateAsync([FromBody][Required] byte[] certificateChain,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(certificateChain);
            await _certificates.AddCertificateChainAsync(certificateChain, true,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// RemoveCertificate
        /// </summary>
        /// <remarks>
        /// Remove a certificate with the provided thumbprint from the specified
        /// store.
        /// </remarks>
        /// <param name="store">The store to add the certificate to</param>
        /// <param name="thumbprint">The thumbprint of the certificate to delete.</param>
        /// <param name="ct"></param>
        [HttpDelete("{store}/certs/{thumbprint}")]
        public async Task RemoveCertificateAsync(CertificateStoreName store,
            string thumbprint, CancellationToken ct = default)
        {
            await _certificates.RemoveCertificateAsync(store, thumbprint,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// RemoveCertificateRevocationList
        /// </summary>
        /// <remarks>
        /// Remove a certificate revocation list from the specified store.
        /// </remarks>
        /// <param name="store">The store to add the certificate to</param>
        /// <param name="crl">The crl to delete.</param>
        /// <param name="ct"></param>
        [HttpDelete("{store}/crls")]
        public async Task RemoveCertificateRevocationListAsync(CertificateStoreName store,
            byte[] crl, CancellationToken ct = default)
        {
            await _certificates.RemoveCertificateRevocationListAsync(store, crl,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// RemoveAll
        /// </summary>
        /// <remarks>
        /// Remove all certificates and revocation lists from the specified
        /// store.
        /// </remarks>
        /// <param name="store">The store to add the certificate to</param>
        /// <param name="ct"></param>
        [HttpDelete("{store}")]
        public async Task RemoveAllAsync(CertificateStoreName store,
            CancellationToken ct = default)
        {
            await _certificates.CleanAsync(store, ct).ConfigureAwait(false);
        }

        private readonly IOpcUaCertificates _certificates;
    }
}
