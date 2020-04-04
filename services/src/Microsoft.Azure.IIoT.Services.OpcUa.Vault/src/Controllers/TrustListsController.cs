// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Api.Vault.Models;
    using Microsoft.Azure.IIoT.OpcUa.Vault;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using System.Linq;

    /// <summary>
    /// Trust lists services.
    /// </summary>
    [ExceptionsFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/trustlists")]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public sealed class TrustListsController : ControllerBase {

        /// <summary>
        /// Create the controller.
        /// </summary>
        /// <param name="services"></param>
        public TrustListsController(ITrustListServices services) {
            _services = services;
        }

        /// <summary>
        /// Add trust relationship
        /// </summary>
        /// <remarks>
        /// Define trust between two entities.  The entities are identifiers
        /// of application, groups, or endpoints.
        /// </remarks>
        /// <param name="entityId">The entity identifier, e.g. group, etc.</param>
        /// <param name="trustedEntityId">The trusted entity identifier</param>
        /// <returns>The group registration</returns>
        [HttpPut("{entityId}/{trustedEntityId}")]
        [Authorize(Policy = Policies.CanManage)]
        public Task AddTrustRelationshipAsync(string entityId,
            string trustedEntityId) {
            return _services.AddTrustRelationshipAsync(entityId, trustedEntityId);
        }

        /// <summary>
        /// List trusted certificates
        /// </summary>
        /// <remarks>
        /// Returns all certificates the entity should trust based on the
        /// applied trust configuration.
        /// </remarks>
        /// <param name="entityId"></param>
        /// <param name="nextPageLink">optional, link to next page</param>
        /// <param name="pageSize">optional, the maximum number of result per page</param>
        [HttpGet("{entityId}")]
        [AutoRestExtension(NextPageLinkName = "nextPageLink")]
        public async Task<X509CertificateListApiModel> ListTrustedCertificatesAsync(string entityId,
            [FromQuery] string nextPageLink, [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                nextPageLink = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            // Use service principal
            HttpContext.User = null; // TODO Set up

            var result = await _services.ListTrustedCertificatesAsync(entityId, nextPageLink,
                pageSize);
            return result.ToApiModel();
        }

        /// <summary>
        /// Remove a trust relationship
        /// </summary>
        /// <remarks>
        /// Removes trust between two entities.  The entities are identifiers
        /// of application, groups, or endpoints.
        /// </remarks>
        /// <param name="entityId">The entity identifier, e.g. group, etc.</param>
        /// <param name="untrustedEntityId">The trusted entity identifier</param>
        /// <returns>The group registration</returns>
        [HttpDelete("{entityId}/{untrustedEntityId}")]
        [Authorize(Policy = Policies.CanManage)]
        public Task RemoveTrustRelationshipAsync(string entityId,
            string untrustedEntityId) {
            return _services.RemoveTrustRelationshipAsync(entityId, untrustedEntityId);
        }

        private readonly ITrustListServices _services;
    }
}
