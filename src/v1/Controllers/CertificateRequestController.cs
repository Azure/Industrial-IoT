// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.CosmosDB.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Models;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Filters;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    /// <inheritdoc/>
    [ApiController]
    [Route(VersionInfo.PATH + "/request"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]
    [Authorize(Policy = Policies.CanRead)]
    public sealed class CertificateRequestController : Controller
    {
        private readonly ICertificateRequest _certificateRequest;
        private readonly IServicesConfig _servicesConfig;

        /// <inheritdoc/>
        public CertificateRequestController(
            ICertificateRequest certificateRequest,
            IServicesConfig servicesConfig)
        {
            _certificateRequest = certificateRequest;
            _servicesConfig = servicesConfig;
        }

        /// <summary>
        /// Start a new signing request.
        /// </summary>
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
        /// Start a new key pair request.
        /// </summary>
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
        /// Approve request.
        /// </summary>
        [HttpPost("{requestId}/{rejected}/approve")]
        [Authorize(Policy = Policies.CanSign)]
        public async Task ApproveCertificateRequestAsync(string requestId, bool rejected)
        {
            // for auto approve the service app id must have signing rights in keyvault
            var onBehalfOfCertificateRequest = await this._certificateRequest.OnBehalfOfRequest(Request);
            await onBehalfOfCertificateRequest.ApproveAsync(requestId, rejected);
        }

        /// <summary>
        /// Accept request.
        /// </summary>
        [HttpPost("{requestId}/accept")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task AcceptCertificateRequestAsync(string requestId)
        {
            await _certificateRequest.AcceptAsync(requestId);
        }

        /// <summary>
        /// Delete request.
        /// </summary>
        [HttpDelete("{requestId}")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DeleteCertificateRequestAsync(string requestId)
        {
            await _certificateRequest.DeleteAsync(requestId);
        }

        /// <summary>
        /// Purge request.
        /// </summary>
        [HttpDelete("{requestId}/purge")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task PurgeCertificateRequestAsync(string requestId)
        {
            // may require elevated rights to delete pk
            await _certificateRequest.PurgeAsync(requestId);
        }

        /// <summary>
        /// Revoke request.
        /// </summary>
        [HttpPost("{requestId}/revoke")]
        [Authorize(Policy = Policies.CanSign)]
        public async Task RevokeCertificateRequestAsync(string requestId)
        {
            var onBehalfOfCertificateRequest = await this._certificateRequest.OnBehalfOfRequest(Request);
            await onBehalfOfCertificateRequest.RevokeAsync(requestId);
        }

        /// <summary>Revoke all deleted requests.</summary>
        [HttpPost("{group}/revokegroup")]
        [Authorize(Policy = Policies.CanSign)]
        public async Task RevokeGroupAsync(string group, bool? allVersions)
        {
            var onBehalfOfCertificateRequest = await this._certificateRequest.OnBehalfOfRequest(Request);
            await onBehalfOfCertificateRequest.RevokeGroupAsync(group, allVersions);
        }

        /// <summary>Query certificate requests</summary>
        [HttpGet("query")]
        public async Task<CertificateRequestRecordQueryResponseApiModel> QueryCertificateRequestsAsync(string appId, string requestState, int? maxResults)
        {
            CertificateRequestState? parsedState = null;
            if (requestState != null)
            {
                parsedState = (CertificateRequestState)Enum.Parse(typeof(CertificateRequestState), requestState);
            }
            ReadRequestResultModel[] results;
            string nextPageLink;
            (nextPageLink, results) = await _certificateRequest.QueryPageAsync(appId, parsedState, null, maxResults);
            return new CertificateRequestRecordQueryResponseApiModel(results, nextPageLink);
        }

        /// <summary>Query certificate requests</summary>
        [HttpPost("query/next")]
        public async Task<CertificateRequestRecordQueryResponseApiModel> QueryCertificateRequestsNextAsync([FromBody] string nextPageLink, string appId, string requestState, int? maxResults)
        {
            CertificateRequestState? parsedState = null;
            if (requestState != null)
            {
                parsedState = (CertificateRequestState)Enum.Parse(typeof(CertificateRequestState), requestState);
            }
            ReadRequestResultModel[] results;
            (nextPageLink, results) = await _certificateRequest.QueryPageAsync(appId, parsedState, nextPageLink, maxResults);
            return new CertificateRequestRecordQueryResponseApiModel(results, nextPageLink);
        }

        /// <summary>Read certificate request</summary>
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

        /// <summary>Fetch certificate request results</summary>
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
    }
}
