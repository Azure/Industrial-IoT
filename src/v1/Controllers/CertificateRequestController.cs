// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Filters;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.Runtime;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    /// <inheritdoc/>
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
        [SwaggerOperation(OperationId = "StartSigningRequest")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<string> StartSigningRequestAsync([FromBody] StartSigningRequestApiModel signingRequest)
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
        [HttpPost("newkeypair")]
        [SwaggerOperation(OperationId = "StartNewKeyPairRequest")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<string> StartNewKeyPairRequestAsync([FromBody] StartNewKeyPairRequestApiModel newKeyPairRequest)
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
        [HttpPost("{requestId}/approve/{rejected}")]
        [SwaggerOperation(OperationId = "ApproveCertificateRequest")]
        [Authorize(Policy = Policies.CanSign)]

        public async Task ApproveCertificateRequestAsync(string requestId, bool rejected)
        {
            // for auto approve the service app id must have signing rights in keyvault
            if (_servicesConfig.AutoApprove)
            {
                await _certificateRequest.ApproveAsync(requestId, rejected);
            }
            else
            {
                var onBehalfOfCertificateRequest = await this._certificateRequest.OnBehalfOfRequest(Request);
                await onBehalfOfCertificateRequest.ApproveAsync(requestId, rejected);
            }
        }

        /// <summary>
        /// Accept request.
        /// </summary>
        [HttpPost("{requestId}/accept")]
        [SwaggerOperation(OperationId = "AcceptCertificateRequest")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task AcceptCertificateRequestAsync(string requestId)
        {
            await _certificateRequest.AcceptAsync(requestId);
        }

        /// <summary>
        /// Delete request.
        /// </summary>
        [HttpDelete("{requestId}")]
        [SwaggerOperation(OperationId = "DeleteCertificateRequest")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DeleteCertificateRequestAsync(string requestId)
        {
            await _certificateRequest.DeleteAsync(requestId);
        }

        /// <summary>
        /// Purge request.
        /// </summary>
        [HttpPost("{requestId}/purge")]
        [SwaggerOperation(OperationId = "PurgeCertificateRequest")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task PurgeCertificateRequestAsync(string requestId)
        {
            await _certificateRequest.PurgeAsync(requestId);
        }

        /// <summary>
        /// Revoke request.
        /// </summary>
        [HttpPost("{requestId}/revoke")]
        [SwaggerOperation(OperationId = "RevokeCertificateRequest")]
        [Authorize(Policy = Policies.CanSign)]
        public async Task RevokeCertificateRequestAsync(string requestId)
        {
            var onBehalfOfCertificateRequest = await this._certificateRequest.OnBehalfOfRequest(Request);
            await onBehalfOfCertificateRequest.RevokeAsync(requestId);
        }

        /// <summary>Revoke all deleted requests.</summary>
        [HttpPost("revoke/{groupId}")]
        [SwaggerOperation(OperationId = "RevokeGroup")]
        [Authorize(Policy = Policies.CanSign)]

        public async Task PostRevokeGroupAsync(string groupId)
        {
            var onBehalfOfCertificateRequest = await this._certificateRequest.OnBehalfOfRequest(Request);
            await onBehalfOfCertificateRequest.RevokeGroupAsync(groupId);
        }

        /// <summary>Query certificate requests</summary>
        [HttpGet]
        [SwaggerOperation(OperationId = "QueryRequests")]
        public async Task<CertificateRequestRecordQueryResponseApiModel> QueryRequestsAsync()
        {
            var results = await _certificateRequest.QueryAsync(null, null);
            return new CertificateRequestRecordQueryResponseApiModel(results);
        }

        /// <summary>Query certificate requests by appId</summary>
        [HttpGet("app/{appId}")]
        [SwaggerOperation(OperationId = "QueryAppRequests")]
        public async Task<CertificateRequestRecordQueryResponseApiModel> QueryAppRequestsAsync(string appId)
        {
            var results = await _certificateRequest.QueryAsync(appId, null);
            return new CertificateRequestRecordQueryResponseApiModel(results);
        }

        /// <summary>Query certificate requests by state</summary>
        [HttpGet("state/{state}")]
        [SwaggerOperation(OperationId = "QueryAppRequests")]
        public async Task<CertificateRequestRecordQueryResponseApiModel> QueryStateRequestsAsync(string state)
        {
            Contract.Requires(string.IsNullOrEmpty(state) == false);
            // todo: parse state
            var results = await _certificateRequest.QueryAsync(null, null);
            return new CertificateRequestRecordQueryResponseApiModel(results);
        }

        /// <summary>Read certificate request</summary>
        [HttpGet("{requestId}")]
        [SwaggerOperation(OperationId = "ReadCertificateRequest")]
        public async Task<CertificateRequestRecordApiModel> ReadCertificateRequestAsync(string requestId)
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

        /// <summary>Complete certificate request</summary>
        [HttpPost("{requestId}/{applicationId}/finish")]
        [SwaggerOperation(OperationId = "FinishRequest")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<FinishRequestApiModel> FinishRequestAsync(string requestId, string applicationId)
        {
            var result = await _certificateRequest.FinishRequestAsync(
                requestId,
                applicationId
                );
            return new FinishRequestApiModel(
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
