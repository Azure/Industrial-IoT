// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Vault;
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.IO;

    /// <summary>
    /// Certificate CRL Distribution Point and Authority Information Access services.
    /// </summary>
    [ExceptionsFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}")]
    [ApiController]
    public sealed class DistributionController : ControllerBase {

        /// <summary>
        /// Create the controller.
        /// </summary>
        /// <param name="services"></param>
        public DistributionController(ICertificateAuthority services) {
            _services = services;
        }

        /// <summary>
        /// Get Issuer Certificate for Authority Information
        /// Access endpoint.
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns>The Issuer Ca cert as a file</returns>
        [HttpGet("issuer/{serialNumber}")]
        [Produces(ContentMimeType.Cert)]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 404)]
        public async Task<ActionResult> GetIssuerCertificateChainAsync(string serialNumber) {
            if (string.IsNullOrEmpty(serialNumber)) {
                throw new ArgumentNullException(nameof(serialNumber));
            }
            try {
                var certificates =
                    await _services.GetIssuerCertificateChainAsync(serialNumber);
                using (var stream = new MemoryStream()) {
                    foreach (var certificate in certificates.Chain) {
                        stream.Write(certificate.Certificate);
                    }
                    return new FileContentResult(stream.ToArray(),
                        ContentMimeType.Cert) {
                        FileDownloadName = serialNumber + ".cer"
                    };
                }
            }
            catch (ResourceNotFoundException) {
                return new NotFoundResult();
            }
        }

        /// <summary>
        /// Get Issuer CRL in CRL Distribution Endpoint.
        /// </summary>
        /// <param name="serialNumber"></param>
        /// <returns></returns>
        [HttpGet("crl/{serialNumber}")]
        [Produces(ContentMimeType.Crl)]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(NotFoundResult), 404)]
        public async Task<ActionResult> GetIssuerCrlChainAsync(string serialNumber) {
            if (string.IsNullOrEmpty(serialNumber)) {
                throw new ArgumentNullException(nameof(serialNumber));
            }
            try {
                var crls =
                    await _services.GetIssuerCrlChainAsync(serialNumber);
                using (var stream = new MemoryStream()) {
                    foreach (var item in crls.Chain) {
                        stream.Write(item.ToRawData());
                    }
                    return new FileContentResult(stream.ToArray(),
                        ContentMimeType.Cert) {
                        FileDownloadName = serialNumber + ".crl"
                    };
                }
            }
            catch (ResourceNotFoundException) {
                return new NotFoundResult();
            }
        }

        private readonly ICertificateAuthority _services;
    }
}
