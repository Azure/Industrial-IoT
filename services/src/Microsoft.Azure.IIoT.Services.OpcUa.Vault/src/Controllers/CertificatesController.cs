// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate services.
    /// </summary>
    [ExceptionsFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/certificates")]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public sealed class CertificatesController : ControllerBase {

        /// <summary>
        /// Create the controller.
        /// </summary>
        /// <param name="services"></param>
        public CertificatesController(ICertificateAuthority services) {
            _services = services;
        }

        /// <summary>
        /// Get Issuer CA Certificate chain.
        /// </summary>
        /// <param name="serialNumber">the serial number of the
        /// Issuer CA Certificate</param>
        /// <returns>The Issuer CA certificate chain</returns>
        [HttpGet("{serialNumber}")]
        [AutoRestExtension(NextPageLinkName = "nextPageLink")]
        public async Task<X509CertificateChainApiModel> GetIssuerCertificateChainAsync(
            string serialNumber) {
            if (string.IsNullOrEmpty(serialNumber)) {
                throw new ArgumentNullException(nameof(serialNumber));
            }
            // Use service principal
            HttpContext.User = null; // TODO Set sp
            var result = await _services.GetIssuerCertificateChainAsync(serialNumber);
            return result.ToApiModel();
        }

        /// <summary>
        /// Get Issuer CA CRL chain.
        /// </summary>
        /// <param name="serialNumber">the serial number of the Issuer
        /// CA Certificate</param>
        /// <returns>The Issuer CA CRL chain</returns>
        [HttpGet("{serialNumber}/crl")]
        public async Task<X509CrlChainApiModel> GetIssuerCrlChainAsync(
            string serialNumber) {
            if (string.IsNullOrEmpty(serialNumber)) {
                throw new ArgumentNullException(nameof(serialNumber));
            }
            // Use service principal
            HttpContext.User = null; // TODO Set sp
            var result = await _services.GetIssuerCrlChainAsync(serialNumber);
            return result.ToApiModel();
        }

        private readonly ICertificateAuthority _services;
    }
}
