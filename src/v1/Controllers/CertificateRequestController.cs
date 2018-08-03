// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Filters;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Controllers
{
    [Route(VersionInfo.PATH + "/request"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]
    public sealed class CertificateRequestController : Controller
    {
        private readonly ICertificateRequest _certificateRequest;

        public CertificateRequestController(ICertificateRequest certificateRequest)
        {
            _certificateRequest = certificateRequest;
        }

        /// <summary>
        /// Start a new signing request.
        /// </summary>
        [HttpPost("sign")]
        [SwaggerOperation(operationId: "StartSigningRequest")]
        public async Task<string> StartSigningRequestAsync([FromBody] StartSigningRequestApiModel signingRequest)
        {
            if (signingRequest == null)
            {
                throw new ArgumentNullException(nameof(signingRequest));
            }
            return await _certificateRequest.StartSigningRequestAsync(
                signingRequest.ApplicationId,
                signingRequest.CertificateGroupId,
                signingRequest.CertificateTypeId,
                signingRequest.ToServiceModel(),
                signingRequest.AuthorityId);
        }

        /// <summary>
        /// Start a new key pair request.
        /// </summary>
        [HttpPost("newkeypair")]
        [SwaggerOperation(operationId: "StartNewKeyPairRequest")]
        public async Task<string> StartNewKeyPairRequestAsync([FromBody] StartNewKeyPairRequestApiModel newKeyPairRequest)
        {
            if (newKeyPairRequest == null)
            {
                throw new ArgumentNullException(nameof(newKeyPairRequest));
            }
            return await _certificateRequest.StartNewKeyPairRequestAsync(
                newKeyPairRequest.ApplicationId,
                newKeyPairRequest.CertificateGroupId,
                newKeyPairRequest.CertificateTypeId,
                newKeyPairRequest.SubjectName,
                newKeyPairRequest.DomainNames,
                newKeyPairRequest.PrivateKeyFormat,
                newKeyPairRequest.PrivateKeyPassword,
                newKeyPairRequest.AuthorityId);
        }

        /// <summary>
        /// Approve request.
        /// </summary>
        [HttpPost("{requestId}/approve/{rejected}")]
        [SwaggerOperation(operationId: "ApproveCertificateRequest")]
        public async Task ApproveCertificateRequestAsync(string requestId, bool rejected)
        {
            await _certificateRequest.ApproveAsync(requestId, rejected);
        }

        /// <summary>
        /// Accept request.
        /// </summary>
        [HttpPost("{requestId}/accept")]
        [SwaggerOperation(operationId: "AcceptCertificateRequest")]
        public async Task AcceptCertificateRequestAsync(string requestId)
        {
            await _certificateRequest.AcceptAsync(requestId);
        }

        /// <summary>Query certificate requests</summary>
        [HttpGet]
        [SwaggerOperation(operationId: "QueryRequests")]
        public async Task<QueryRequestsResponseApiModel> QueryRequestsAsync()
        {
            var results = await _certificateRequest.QueryAsync(null, null);
            return new QueryRequestsResponseApiModel(results);
        }

        /// <summary>Query certificate requests by appId</summary>
        [HttpGet("app/{appId}")]
        [SwaggerOperation(operationId: "QueryAppRequests")]
        public async Task<QueryRequestsResponseApiModel> QueryAppRequestsAsync(string appId)
        {
            var results = await _certificateRequest.QueryAsync(appId, null);
            return new QueryRequestsResponseApiModel(results);
        }

        /// <summary>Query certificate requests by state</summary>
        [HttpGet("state/{state}")]
        [SwaggerOperation(operationId: "QueryAppRequests")]
        public async Task<QueryRequestsResponseApiModel> QueryStateRequestsAsync(string state)
        {
            // todo: parse state
            var results = await _certificateRequest.QueryAsync(null, null);
            return new QueryRequestsResponseApiModel(results);
        }

        /// <summary>Read certificate request</summary>
        [HttpGet("{requestId}")]
        [SwaggerOperation(operationId: "ReadCertificateRequest")]
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
        [SwaggerOperation(operationId: "FinishRequest")]
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
