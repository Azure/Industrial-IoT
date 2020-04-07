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
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Microsoft.Azure.IIoT.AspNetCore.OpenApi;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Certificate request services.
    /// </summary>
    [ExceptionsFilter]
    [ApiVersion("2")]
    [Route("v{version:apiVersion}/requests")]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    public sealed class RequestsController : ControllerBase {

        /// <summary>
        /// Create controller with services
        /// </summary>
        /// <param name="signing">certificate services</param>
        /// <param name="keyPair"></param>
        /// <param name="management"></param>
        public RequestsController(ISigningRequestProcessor signing,
            IKeyPairRequestProcessor keyPair, IRequestManagement management) {
            _signing = signing;
            _keyPair = keyPair;
            _management = management;
        }

        /// <summary>
        /// Create a certificate request with a certificate signing request (CSR).
        /// </summary>
        /// <remarks>
        /// The request is in the 'New' state after this call.
        /// Requires Writer or Manager role.
        /// </remarks>
        /// <param name="signingRequest">The signing request parameters</param>
        /// <returns>The certificate request id</returns>
        [HttpPut("sign")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<StartSigningRequestResponseApiModel> StartSigningRequestAsync(
            [FromBody] [Required] StartSigningRequestApiModel signingRequest) {
            if (signingRequest == null) {
                throw new ArgumentNullException(nameof(signingRequest));
            }
            HttpContext.User = null; // TODO: Set service principal
            var result = await _signing.StartSigningRequestAsync(
                signingRequest.ToServiceModel(), new VaultOperationContextModel {
                    AuthorityId = User.Identity.Name,
                    Time = DateTime.UtcNow
                });
            return result.ToApiModel();
        }

        /// <summary>
        /// Fetch signing request results.
        /// </summary>
        /// <remarks>
        /// Can be called in any state.
        /// After a successful fetch in 'Completed' state, the request is
        /// moved into 'Accepted' state.
        /// Requires Writer role.
        /// </remarks>
        /// <param name="requestId"></param>
        /// <returns>
        /// The state, the issued Certificate and the private key, if available.
        /// </returns>
        [HttpGet("sign/{requestId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<FinishSigningRequestResponseApiModel> FinishSigningRequestAsync(
            string requestId) {
            HttpContext.User = null; // TODO: Set service principal
            var result = await _signing.FinishSigningRequestAsync(requestId,
                new VaultOperationContextModel {
                    AuthorityId = User.Identity.Name,
                    Time = DateTime.UtcNow
                });
            return result.ToApiModel();
        }

        /// <summary>
        /// Create a certificate request with a new key pair.
        /// </summary>
        /// <remarks>
        /// The request is in the 'New' state after this call.
        /// Requires Writer or Manager role.
        /// </remarks>
        /// <param name="newKeyPairRequest">The new key pair request parameters
        /// </param>
        /// <returns>The certificate request id</returns>
        [HttpPut("keypair")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<StartNewKeyPairRequestResponseApiModel> StartNewKeyPairRequestAsync(
            [FromBody] [Required] StartNewKeyPairRequestApiModel newKeyPairRequest) {
            if (newKeyPairRequest == null) {
                throw new ArgumentNullException(nameof(newKeyPairRequest));
            }
            HttpContext.User = null; // TODO: Set service principal
            var result = await _keyPair.StartNewKeyPairRequestAsync(
                newKeyPairRequest.ToServiceModel(),
                new VaultOperationContextModel {
                    AuthorityId = User.Identity.Name,
                    Time = DateTime.UtcNow
                });
            return result.ToApiModel();
        }

        /// <summary>
        /// Fetch certificate request result.
        /// </summary>
        /// <remarks>
        /// Can be called in any state.
        /// Fetches private key in 'Completed' state.
        /// After a successful fetch in 'Completed' state, the request is
        /// moved into 'Accepted' state.
        /// Requires Writer role.
        /// </remarks>
        /// <param name="requestId"></param>
        /// <returns>
        /// The state, the issued Certificate and the private key, if available.
        /// </returns>
        [HttpGet("keypair/{requestId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<FinishNewKeyPairRequestResponseApiModel> FinishNewKeyPairRequestAsync(
            string requestId) {
            HttpContext.User = null; // TODO: Set service principal
            var result = await _keyPair.FinishNewKeyPairRequestAsync(requestId,
                new VaultOperationContextModel {
                    AuthorityId = User.Identity.Name,
                    Time = DateTime.UtcNow
                });
            return result.ToApiModel();
        }

        /// <summary>
        /// Approve the certificate request.
        /// </summary>
        /// <remarks>
        /// Validates the request with the application database.
        ///- If Approved:
        ///  - New Key Pair request: Creates the new key pair
        ///        in the requested format, signs the certificate and stores the
        ///        private key for later securely in KeyVault.
        ///  - Cert Signing Request: Creates and signs the certificate.
        ///        Deletes the CSR from the database.
        /// Stores the signed certificate for later use in the Database.
        /// The request is in the 'Approved' or 'Rejected' state after this call.
        /// Requires Approver role.
        /// Approver needs signing rights in KeyVault.
        /// </remarks>
        /// <param name="requestId">The certificate request id</param>
        /// <returns></returns>
        [HttpPost("{requestId}/approve")]
        [Authorize(Policy = Policies.CanSign)]
        public async Task ApproveRequestAsync(string requestId) {
            // for auto approve the service app id must have signing rights in keyvault
            await _management.ApproveRequestAsync(requestId, new VaultOperationContextModel {
                AuthorityId = User.Identity.Name,
                Time = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Reject the certificate request.
        /// </summary>
        /// <remarks>
        /// The request is in the 'Rejected' state after this call.
        /// Requires Approver role.
        /// Approver needs signing rights in KeyVault.
        /// </remarks>
        /// <param name="requestId">The certificate request id</param>
        /// <returns></returns>
        [HttpPost("{requestId}/reject")]
        [Authorize(Policy = Policies.CanSign)]
        public async Task RejectRequestAsync(string requestId) {
            // for auto approve the service app id must have signing rights in keyvault
            await _management.RejectRequestAsync(requestId, new VaultOperationContextModel {
                AuthorityId = User.Identity.Name,
                Time = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Cancel request
        /// </summary>
        /// <remarks>
        /// The request is in the 'Accepted' state after this call.
        /// Requires Writer role.
        /// </remarks>
        /// <param name="requestId">The certificate request id</param>
        [HttpPost("{requestId}/accept")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task AcceptRequestAsync(string requestId) {
            HttpContext.User = null; // TODO: Set service principal
            await _management.AcceptRequestAsync(requestId, new VaultOperationContextModel {
                AuthorityId = User.Identity.Name,
                Time = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Delete request. Physically delete the request.
        /// </summary>
        /// <remarks>
        /// By purging the request it is actually physically deleted from the
        /// database, including the public key and other information.
        /// Requires Manager role.
        /// </remarks>
        /// <param name="requestId">The certificate request id</param>
        [HttpDelete("{requestId}")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DeleteRequestAsync(string requestId) {
            // may require elevated rights to delete pk
            HttpContext.User = null; // TODO: Set service principal
            await _management.DeleteRequestAsync(requestId, new VaultOperationContextModel {
                AuthorityId = User.Identity.Name,
                Time = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get a specific certificate request.
        /// </summary>
        /// <param name="requestId">The certificate request id</param>
        /// <returns>The certificate request</returns>
        [HttpGet("{requestId}")]
        public async Task<CertificateRequestRecordApiModel> GetRequestAsync(
            string requestId) {
            HttpContext.User = null; // TODO: Set service principal
            var result = await _management.GetRequestAsync(requestId);
            return result.ToApiModel();
        }

        /// <summary>
        /// Query for certificate requests.
        /// </summary>
        /// <remarks>
        /// Get all certificate requests in paged form.
        /// The returned model can contain a link to the next page if more results are
        /// available.  Use ListRequests to continue.
        /// </remarks>
        /// <param name="query">optional, query filter</param>
        /// <param name="pageSize">optional, the maximum number of result per page</param>
        /// <returns>Matching requests, next page link</returns>
        [HttpPost("query")]
        public async Task<CertificateRequestQueryResponseApiModel> QueryRequestsAsync(
            [FromBody] CertificateRequestQueryRequestApiModel query,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }

            HttpContext.User = null; // TODO: Set service principal
            var result = await _management.QueryRequestsAsync(query?.ToServiceModel(),
                pageSize);
            return result.ToApiModel();
        }

        /// <summary>
        /// Lists certificate requests.
        /// </summary>
        /// <remarks>
        /// Get all certificate requests in paged form or continue a current listing or
        /// query.
        /// The returned model can contain a link to the next page if more results are
        /// available.
        /// </remarks>
        /// <param name="nextPageLink">optional, link to next page </param>
        /// <param name="pageSize">optional, the maximum number of result per page</param>
        /// <returns>Matching requests, next page link</returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "nextPageLink")]
        public async Task<CertificateRequestQueryResponseApiModel> ListRequestsAsync(
            [FromQuery] string nextPageLink, [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                nextPageLink = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }

            HttpContext.User = null; // TODO: Set service principal
            var result = await _management.ListRequestsAsync(
                nextPageLink, pageSize);
            return result.ToApiModel();
        }

        private readonly ISigningRequestProcessor _signing;
        private readonly IKeyPairRequestProcessor _keyPair;
        private readonly IRequestManagement _management;
    }
}
