// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.Http;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Swagger;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Filters;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    /// <summary>
    /// Certificate Request services.
    /// </summary>
    [ApiController]
    [Route(VersionInfo.PATH + "/request"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]
    [Authorize(Policy = Policies.CanRead)]
    public sealed class CertificateRequestController : Controller
    {
        private readonly ICertificateRequest _certificateRequest;
        private readonly IServicesConfig _servicesConfig;

        /// <summary>
        /// Create controller with service.
        /// </summary>
        /// <param name="certificateRequest"></param>
        /// <param name="servicesConfig"></param>
        public CertificateRequestController(
            ICertificateRequest certificateRequest,
            IServicesConfig servicesConfig)
        {
            _certificateRequest = certificateRequest;
            _servicesConfig = servicesConfig;
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
        [HttpPost("sign")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<string> CreateSigningRequestAsync([FromBody] CreateSigningRequestApiModel signingRequest)
        {
            if (signingRequest == null)
            {
                throw new ArgumentNullException(nameof(signingRequest));
            }
            string authorityId = User.Identity.Name;
            return await this._certificateRequest.StartSigningRequestAsync(
                signingRequest.ApplicationId,
                signingRequest.CertificateGroupId,
                signingRequest.CertificateTypeId,
                signingRequest.ToServiceModel(),
                authorityId);
        }

        /// <summary>
        /// Create a certificate request with a new key pair.
        /// </summary>
        /// <remarks>
        /// The request is in the 'New' state after this call.
        /// Requires Writer or Manager role.
        /// </remarks>
        /// <param name="newKeyPairRequest">The new key pair request parameters</param>
        /// <returns>The certificate request id</returns>
        [HttpPost("keypair")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<string> CreateNewKeyPairRequestAsync([FromBody] CreateNewKeyPairRequestApiModel newKeyPairRequest)
        {
            if (newKeyPairRequest == null)
            {
                throw new ArgumentNullException(nameof(newKeyPairRequest));
            }
            string authorityId = User.Identity.Name;
            return await _certificateRequest.StartNewKeyPairRequestAsync(
                newKeyPairRequest.ApplicationId,
                newKeyPairRequest.CertificateGroupId,
                newKeyPairRequest.CertificateTypeId,
                newKeyPairRequest.SubjectName,
                newKeyPairRequest.DomainNames,
                newKeyPairRequest.PrivateKeyFormat,
                newKeyPairRequest.PrivateKeyPassword,
                authorityId);
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
        /// <param name="rejected">if the request is rejected(true) or approved(false)</param>
        /// <returns></returns>
        [HttpPost("{requestId}/{rejected}/approve")]
        [Authorize(Policy = Policies.CanSign)]
        public async Task ApproveCertificateRequestAsync(string requestId, bool rejected)
        {
            // for auto approve the service app id must have signing rights in keyvault
            var onBehalfOfCertificateRequest = await this._certificateRequest.OnBehalfOfRequest(Request);
            await onBehalfOfCertificateRequest.ApproveAsync(requestId, rejected);
        }

        /// <summary>
        /// Accept request and delete the private key.
        /// </summary>
        /// <remarks>
        /// By accepting the request the requester takes ownership of the certificate
        /// and the private key, if requested. A private key with metadata is deleted from KeyVault.
        /// The public certificate remains in the database for sharing public key information
        /// or for later revocation once the application is deleted.
        /// The request is in the 'Accepted' state after this call.
        /// Requires Writer role.
        /// </remarks>
        /// <param name="requestId">The certificate request id</param>
        [HttpPost("{requestId}/accept")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task AcceptCertificateRequestAsync(string requestId)
        {
            await _certificateRequest.AcceptAsync(requestId);
        }

        /// <summary>
        /// Delete request. Mark the certificate for revocation.
        /// </summary>
        /// <remarks>
        /// If the request is in the 'Approved' or 'Accepted' state, 
        /// the request is set in the 'Deleted' state.
        /// A deleted request is marked for revocation.
        /// The public certificate is still available for the revocation procedure.
        /// If the request is in the 'New' or 'Rejected' state, 
        /// the request is set in the 'Removed' state.
        /// The request is in the 'Deleted' or 'Removed'state after this call.
        /// Requires Manager role.
        /// </remarks>
        /// <param name="requestId">The certificate request id</param>
        [HttpDelete("{requestId}")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DeleteCertificateRequestAsync(string requestId)
        {
            await _certificateRequest.DeleteAsync(requestId);
        }

        /// <summary>
        /// Purge request. Physically delete the request.
        /// </summary>
        /// <remarks>
        /// The request must be in the 'Revoked','Rejected' or 'New' state.
        /// By purging the request it is actually physically deleted from the
        /// database, including the public key and other information.
        /// The request is purged after this call.
        /// Requires Manager role.
        /// </remarks>
        /// <param name="requestId">The certificate request id</param>
        [HttpDelete("{requestId}/purge")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task PurgeCertificateRequestAsync(string requestId)
        {
            // may require elevated rights to delete pk
            await _certificateRequest.PurgeAsync(requestId);
        }

        /// <summary>
        /// Revoke request. Create New CRL version with revoked certificate.
        /// </summary>
        /// <remarks>
        /// The request must be in the 'Deleted' state for revocation.
        /// The certificate issuer CA and CRL are looked up, the certificate
        /// serial number is added and a new CRL version is issued and updated
        /// in the certificate group storage.
        /// Preferably deleted certificates are revoked with the RevokeGroup
        /// call to batch multiple revoked certificates in a single CRL update.
        /// Requires Approver role.
        /// Approver needs signing rights in KeyVault.
        /// </remarks>
        /// <param name="requestId">The certificate request id</param>
        [HttpPost("{requestId}/revoke")]
        [Authorize(Policy = Policies.CanSign)]
        public async Task RevokeCertificateRequestAsync(string requestId)
        {
            var onBehalfOfCertificateRequest = await _certificateRequest.OnBehalfOfRequest(Request);
            await onBehalfOfCertificateRequest.RevokeAsync(requestId);
        }

        /// <summary>
        /// Revoke all deleted certificate requests for a group.
        /// </summary>
        /// <remarks>
        /// Select all requests for a group in the 'Deleted' state are marked
        /// for revocation.
        /// The certificate issuer CA and CRL are looked up, all the certificate
        /// serial numbers are added and a new CRL version is issued and updated
        /// in the certificate group storage.
        /// Requires Approver role.
        /// Approver needs signing rights in KeyVault.
        /// </remarks>
        /// <param name="group">The certificate group id</param>
        /// <param name="allVersions">optional, if all certs for all CA versions should be revoked. Default: true</param>
        /// <returns></returns>
        [HttpPost("{group}/revokegroup")]
        [Authorize(Policy = Policies.CanSign)]
        public async Task RevokeCertificateGroupAsync(string group, bool? allVersions)
        {
            var onBehalfOfCertificateRequest = await _certificateRequest.OnBehalfOfRequest(Request);
            await onBehalfOfCertificateRequest.RevokeGroupAsync(group, allVersions ?? true);
        }

        /// <summary>
        /// Query for certificate requests.
        /// </summary>
        /// <remarks>
        /// Get all certificate requests in paged form.
        /// The returned model can contain a link to the next page if more results are
        /// available.
        /// </remarks>
        /// <param name="appId">optional, query for application id</param>
        /// <param name="requestState">optional, query for request state</param>
        /// <param name="nextPageLink">optional, link to next page </param>
        /// <param name="pageSize">optional, the maximum number of result per page</param>
        /// <returns>Matching requests, next page link</returns>
        [HttpGet("query")]
        [AutoRestExtension(NextPageLinkName = "nextPageLink")]
        public async Task<CertificateRequestQueryResponseApiModel> QueryCertificateRequestsAsync(
            string appId,
            CertificateRequestState? requestState,
            [FromQuery] string nextPageLink,
            [FromQuery] int? pageSize)
        {
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount))
            {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            ReadRequestResultModel[] results;
            (nextPageLink, results) = await _certificateRequest.QueryPageAsync(
                appId,
                (Types.CertificateRequestState?)requestState,
                nextPageLink,
                pageSize);
            return new CertificateRequestQueryResponseApiModel(results, nextPageLink);
        }

        /// <summary>
        /// Get a specific certificate request.
        /// </summary>
        /// <param name="requestId">The certificate request id</param>
        /// <returns>The certificate request</returns>
        [HttpGet("{requestId}")]
        public async Task<CertificateRequestRecordApiModel> GetCertificateRequestAsync(string requestId)
        {
            var result = await _certificateRequest.ReadAsync(requestId);
            return new CertificateRequestRecordApiModel(
                requestId,
                result.ApplicationId,
                result.State,
                result.CertificateGroupId,
                result.CertificateTypeId,
                result.SigningRequest,
                result.SubjectName,
                result.DomainNames,
                result.PrivateKeyFormat);
        }

        /// <summary>
        /// Fetch certificate request approval result. 
        /// </summary>
        /// <remarks>
        /// Can be called in any state.
        /// Returns only cert request information in 'New', 'Rejected',
        /// 'Deleted' and 'Revoked' state.
        /// Fetches private key in 'Approved' state, if requested.
        /// Fetches the public certificate in 'Approved' and 'Accepted' state.
        /// After a successful fetch in 'Approved' state, the request should be
        /// accepted to delete the private key.
        /// Requires Writer role.
        /// </remarks>
        /// <param name="requestId"></param>
        /// <param name="applicationId"></param>
        /// <returns>
        /// The state, the issued Certificate and the private key, if available.
        /// </returns>
        [HttpGet("{requestId}/{applicationId}/fetch")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<FetchRequestResultApiModel> FetchCertificateRequestResultAsync(string requestId, string applicationId)
        {
            var result = await _certificateRequest.FetchRequestAsync(
                requestId,
                applicationId
                );
            return new FetchRequestResultApiModel(
                requestId,
                applicationId,
                result.State,
                result.CertificateGroupId,
                result.CertificateTypeId,
                result.SignedCertificate,
                result.PrivateKeyFormat,
                result.PrivateKey,
                result.AuthorityId
                );
        }

        /// <summary>
        /// Parse the certificate state parameter.
        /// </summary>
        private CertificateRequestState? ParseCertificateState(string requestState)
        {
            CertificateRequestState? parsedState = null;
            if (!String.IsNullOrWhiteSpace(requestState))
            {
                object tryParse;
                if (!Enum.TryParse(typeof(CertificateRequestState), requestState, true, out tryParse))
                {
                    throw new ArgumentOutOfRangeException(nameof(requestState), "The argument must be a valid state of a CertificateRequest.");
                };
                parsedState = (CertificateRequestState)tryParse;
            }
            return parsedState;
        }
    }
}
