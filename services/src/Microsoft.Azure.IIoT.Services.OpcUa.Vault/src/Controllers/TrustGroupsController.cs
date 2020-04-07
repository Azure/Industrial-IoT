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
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    /// <summary>
    /// Trust group services.
    /// </summary>
    [ExceptionsFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/groups")]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public sealed class TrustGroupsController : ControllerBase {

        /// <summary>
        /// Create the controller.
        /// </summary>
        /// <param name="groups">Policy store client</param>
        /// <param name="services"></param>
        public TrustGroupsController(ITrustGroupStore groups, ITrustGroupServices services) {
            _groups = groups;
            _services = services;
        }

        /// <summary>
        /// Get information about all groups.
        /// </summary>
        /// <remarks>
        /// A trust group has a root certificate which issues certificates
        /// to entities.  Entities can be part of a trust group and thus
        /// trust the root certificate and all entities that the root has
        /// issued certificates for.
        /// </remarks>
        /// <param name="nextPageLink">optional, link to next page</param>
        /// <param name="pageSize">optional, the maximum number of result per page</param>
        /// <returns>The configurations</returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "nextPageLink")]
        public async Task<TrustGroupRegistrationListApiModel> ListGroupsAsync(
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
            HttpContext.User = null; // TODO Set sp
            var config = await _groups.ListGroupsAsync(nextPageLink, pageSize);
            return config.ToApiModel();
        }

        /// <summary>
        /// Get group information.
        /// </summary>
        /// <remarks>
        /// A trust group has a root certificate which issues certificates
        /// to entities.  Entities can be part of a trust group and thus
        /// trust the root certificate and all entities that the root has
        /// issued certificates for.
        /// </remarks>
        /// <param name="groupId">The group id</param>
        /// <returns>The configuration</returns>
        [HttpGet("{groupId}")]
        public async Task<TrustGroupRegistrationApiModel> GetGroupAsync(
            string groupId) {
            var group = await _groups.GetGroupAsync(groupId);
            return group.ToApiModel();
        }

        /// <summary>
        /// Update group registration.
        /// </summary>
        /// <remarks>
        /// Use this function with care and only if you are aware of
        /// the security implications.
        /// Requires manager role.
        /// </remarks>
        /// <param name="groupId">The group id</param>
        /// <param name="request">The group configuration</param>
        /// <returns>The configuration</returns>
        [HttpPost("{groupId}")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task UpdateGroupAsync(string groupId,
            [FromBody] [Required] TrustGroupUpdateRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _groups.UpdateGroupAsync(groupId, request.ToServiceModel());
        }

        /// <summary>
        /// Create new root group.
        /// </summary>
        /// <remarks>
        /// Requires manager role.
        /// </remarks>
        /// <param name="request">The create request</param>
        /// <returns>The group registration response</returns>
        [HttpPut("root")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<TrustGroupRegistrationResponseApiModel> CreateRootAsync(
            [FromBody] [Required] TrustGroupRootCreateRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _groups.CreateRootAsync(request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Create new sub-group of an existing group.
        /// </summary>
        /// <remarks>
        /// Requires manager role.
        /// </remarks>
        /// <param name="request">The create request</param>
        /// <returns>The group registration response</returns>
        [HttpPut]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<TrustGroupRegistrationResponseApiModel> CreateGroupAsync(
            [FromBody] [Required] TrustGroupRegistrationRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _groups.CreateGroupAsync(request.ToServiceModel());
            return result.ToApiModel();
        }

        /// <summary>
        /// Renew a group CA Certificate.
        /// </summary>
        /// <remark>
        /// A new key and CA cert is created for the group.
        /// The new issuer cert and CRL become active immediately
        /// for signing.
        /// All newly approved certificates are signed with the new key.
        /// </remark>
        /// <param name="groupId"></param>
        /// <returns>The new Issuer CA certificate</returns>
        [HttpPost("{groupId}/renew")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task RenewIssuerCertificateAsync(string groupId) {
            await _services.RenewCertificateAsync(groupId);
        }

        /// <summary>
        /// Delete a group.
        /// </summary>
        /// <remarks>
        /// After this operation the Issuer CA, CRLs and keys become inaccessible.
        /// Use this function with extreme caution.
        /// Requires manager role.
        /// </remarks>
        /// <param name="groupId">The group id</param>
        /// <returns></returns>
        [HttpDelete("{groupId}")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DeleteGroupAsync(string groupId) {
            await _groups.DeleteGroupAsync(groupId);
        }

        private readonly ITrustGroupStore _groups;
        private readonly ITrustGroupServices _services;
    }
}
