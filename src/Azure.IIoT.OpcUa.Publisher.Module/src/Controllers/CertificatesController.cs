// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Module.Controllers
{
    using Asp.Versioning;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Azure.IIoT.OpcUa.Publisher.Module.Filters;
    using Azure.IIoT.OpcUa.Publisher.Stack;
    using Furly;
    using Furly.Tunnel.Router;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
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
    [Authorize]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
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
        /// <exception cref="ArgumentException">if store name is invalid.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information such as store name
        /// is invalid</response>
        /// <response code="404">Nothing could be found.</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet("{store}/certs")]
        public async Task<IReadOnlyList<X509CertificateModel>> ListCertificatesAsync(
            string store, CancellationToken ct = default)
        {
            if (!Enum.TryParse<CertificateStoreName>(store, out var storeType))
            {
                throw new ArgumentException("Invalid store name");
            }
            return await _certificates.ListCertificatesAsync(storeType, false,
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
        /// <exception cref="ArgumentException">if store name is invalid.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information such as store name
        /// is invalid</response>
        /// <response code="404">Nothing could be found.</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet("{store}/crls")]
        public async Task<IReadOnlyList<byte[]>> ListCertificateRevocationListsAsync(
            string store, CancellationToken ct = default)
        {
            if (!Enum.TryParse<CertificateStoreName>(store, out var storeType))
            {
                throw new ArgumentException("Invalid store name");
            }
            return await _certificates.ListCertificateRevocationListsAsync(storeType,
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
        /// <exception cref="ArgumentException">if store name is invalid.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information such as store name
        /// is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPatch("{store}/certs")]
        public async Task AddCertificateAsync(string store,
            [FromBody][Required] byte[] pfxBlob, [FromQuery] string? password,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(pfxBlob);
            if (!Enum.TryParse<CertificateStoreName>(store, out var storeType))
            {
                throw new ArgumentException("Invalid store name");
            }
            await _certificates.AddCertificateAsync(storeType, pfxBlob, password,
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
        /// <exception cref="ArgumentException">if store name is invalid.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPatch("{store}/crls")]
        public async Task AddCertificateRevocationListAsync(string store,
            [FromBody][Required] byte[] crl, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(crl);
            if (!Enum.TryParse<CertificateStoreName>(store, out var storeType))
            {
                throw new ArgumentException("Invalid store name");
            }
            await _certificates.AddCertificateRevocationListAsync(storeType, crl,
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
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
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
        /// <exception cref="ArgumentException">if store name is invalid.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information such store name is invalid</response>
        /// <response code="404">Nothing could be found.</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpDelete("{store}/certs/{thumbprint}")]
        public async Task RemoveCertificateAsync(string store,
            string thumbprint, CancellationToken ct = default)
        {
            if (!Enum.TryParse<CertificateStoreName>(store, out var storeType))
            {
                throw new ArgumentException("Invalid store name");
            }
            await _certificates.RemoveCertificateAsync(storeType, thumbprint,
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
        /// <exception cref="ArgumentException">if store name is invalid.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information such store name is invalid</response>
        /// <response code="404">Nothing could be found.</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpDelete("{store}/crls")]
        public async Task RemoveCertificateRevocationListAsync(string store,
            byte[] crl, CancellationToken ct = default)
        {
            if (!Enum.TryParse<CertificateStoreName>(store, out var storeType))
            {
                throw new ArgumentException("Invalid store name");
            }
            await _certificates.RemoveCertificateRevocationListAsync(storeType, crl,
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
        /// <exception cref="ArgumentException">if store name is invalid.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information such store name is invalid</response>
        /// <response code="404">Nothing could be found.</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpDelete("{store}")]
        public async Task RemoveAllAsync(string store, CancellationToken ct = default)
        {
            if (!Enum.TryParse<CertificateStoreName>(store, out var storeType))
            {
                throw new ArgumentException("Invalid store name");
            }
            await _certificates.CleanAsync(storeType, ct).ConfigureAwait(false);
        }

        private readonly IOpcUaCertificates _certificates;
    }
}
