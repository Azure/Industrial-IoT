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
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// <para>
    /// This section lists the general APi provided by OPC Publisher providing
    /// all connection, endpoint and address space related API methods.
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
        /// Get the certificates in the specified certificated store
        /// </remarks>
        /// <param name="store">The store to enumerate</param>
        /// <param name="ct"></param>
        /// <returns>The list of certificates currently in the store.</returns>
        [HttpGet("{store}")]
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
        [HttpGet("{store}/crl")]
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
        /// <param name="password">The optional password of the blob</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="pfxBlob"/>
        /// is <c>null</c>.</exception>
        [HttpPut("{store}")]
        public async Task AddCertificateAsync(CertificateStoreName store,
            byte[] pfxBlob, [FromQuery] string? password, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pfxBlob);
            await _certificates.AddCertificateAsync(store, pfxBlob, password,
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
        /// <param name="store">The store to add the certificate to</param>
        /// <param name="certificateChain">The certificate chain.</param>
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="certificateChain"/>
        /// is <c>null</c>.</exception>
        [HttpPut("{store}/chain")]
        public async Task AddCertificateChainAsync(CertificateStoreName store,
            byte[] certificateChain, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(certificateChain);
            await _certificates.AddCertificateChainAsync(store, certificateChain,
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
        [HttpDelete("{store}/{thumbprint}")]
        public async Task RemoveCertificateAsync(CertificateStoreName store,
            string thumbprint, CancellationToken ct = default)
        {
            await _certificates.RemoveCertificateAsync(store, thumbprint,
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
        [HttpPost("approve/{thumbprint}")]
        public async Task ApproveRejectedCertificateAsync(string thumbprint,
            CancellationToken ct = default)
        {
            await _certificates.ApproveRejectedCertificateAsync(thumbprint,
                ct).ConfigureAwait(false);
        }

        private readonly IOpcUaCertificates _certificates;
    }
}
