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
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    [Route(VersionInfo.PATH + "/groups"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]
    [Authorize(Policy = Policies.CanRead)]

    public sealed class CertificateGroupController : Controller
    {
        private readonly ICertificateGroup certificateGroups;

        public CertificateGroupController(
            ICertificateGroup certificateGroups)
        {
            this.certificateGroups = certificateGroups;
        }

        /// <returns>List of certificate groups</returns>
        [HttpGet]
        [SwaggerOperation(OperationId = "GetCertificateGroupIds")]
        public async Task<CertificateGroupListApiModel> GetAsync()
        {
            return new CertificateGroupListApiModel(await this.certificateGroups.GetCertificateGroupIds());
        }

        /// <summary>Get group configuration</summary>
        [HttpGet("{groupId}")]
        [SwaggerOperation(OperationId = "GetCertificateGroupConfiguration")]
        public async Task<CertificateGroupConfigurationApiModel> GetAsync(string groupId)
        {
            return new CertificateGroupConfigurationApiModel(
                groupId,
                await this.certificateGroups.GetCertificateGroupConfiguration(groupId));
        }

        /// <summary>Update group configuration</summary>
        [HttpPut("{groupId}")]
        [SwaggerOperation(OperationId = "UpdateCertificateGroupConfiguration")]
        [Authorize(Policy = Policies.CanManage)]

        public async Task<CertificateGroupConfigurationApiModel> PutAsync(string groupId, [FromBody] CertificateGroupConfigurationApiModel config)
        {
            var onBehalfOfCertificateGroups = await this.certificateGroups.OnBehalfOfRequest(Request);
            return new CertificateGroupConfigurationApiModel(
                groupId,
                await onBehalfOfCertificateGroups.UpdateCertificateGroupConfiguration(groupId, config.ToServiceModel()));
        }

        /// <summary>Create new group configuration</summary>
        [HttpPost("{groupId}/{subject}/{certType}")]
        [SwaggerOperation(OperationId = "CreateCertificateGroupConfiguration")]
        [Authorize(Policy = Policies.CanManage)]

        public async Task<CertificateGroupConfigurationApiModel> PostAsync(string groupId, string subject, string certType)
        {
            var onBehalfOfCertificateGroups = await this.certificateGroups.OnBehalfOfRequest(Request);
            return new CertificateGroupConfigurationApiModel(
                groupId,
                await onBehalfOfCertificateGroups.CreateCertificateGroupConfiguration(groupId, subject, certType));
        }

        /// <summary>Get group configuration</summary>
        [HttpGet("config")]
        [SwaggerOperation(OperationId = "GetCertificateGroupConfigurationCollection")]
        public async Task<CertificateGroupConfigurationCollectionApiModel> GetConfigAsync()
        {
            return new CertificateGroupConfigurationCollectionApiModel(
                await this.certificateGroups.GetCertificateGroupConfigurationCollection());
        }

        /// <summary>Get CA Certificate chain</summary>
        [HttpGet("{groupId}/cacert")]
        [SwaggerOperation(OperationId = "GetCACertificateChain")]
        public async Task<X509Certificate2CollectionApiModel> GetCACertificateChainAsync(string groupId)
        {
            return new X509Certificate2CollectionApiModel(
                await this.certificateGroups.GetCACertificateChainAsync(groupId));
        }

        /// <summary>Get CA CRL chain</summary>
        [HttpGet("{groupId}/cacrl")]
        [SwaggerOperation(OperationId = "GetCACrlChain")]
        public async Task<X509CrlCollectionApiModel> GetCACrlChainAsync(string groupId)
        {
            return new X509CrlCollectionApiModel(
                await this.certificateGroups.GetCACrlChainAsync(groupId));
        }

        /// <summary>Get trust list</summary>
        [HttpGet("{groupId}/trustlist")]
        [SwaggerOperation(OperationId = "GetTrustList")]
        public async Task<TrustListApiModel> GetTrustListAsync(string groupId)
        {
            return new TrustListApiModel(await this.certificateGroups.GetTrustListAsync(groupId));
        }

        /// <summary>Create new CA Certificate</summary>
        [HttpPost("{groupId}/create")]
        [SwaggerOperation(OperationId = "CreateCACertificate")]
        [Authorize(Policy = Policies.CanManage)]

        public async Task<X509Certificate2ApiModel> PostCreateAsync(string groupId)
        {
            var onBehalfOfCertificateGroups = await this.certificateGroups.OnBehalfOfRequest(Request);
            return new X509Certificate2ApiModel(
                await onBehalfOfCertificateGroups.CreateCACertificateAsync(groupId));
        }

#if CERTSIGNER
        /// <summary>Revoke Certificate</summary>
        [HttpPost("{groupId}/revoke")]
        [SwaggerOperation(OperationId = "RevokeCertificate")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<X509CrlApiModel> PostRevokeAsync(string groupId, [FromBody] X509Certificate2ApiModel cert)
        {
            var onBehalfOfCertificateGroups = await this.certificateGroups.OnBehalfOfRequest(Request);
            return new X509CrlApiModel(
                await onBehalfOfCertificateGroups.RevokeCertificateAsync(
                    groupId,
                    cert.ToServiceModel()));
        }

        /// <summary>Signing Request</summary>
        [HttpPost("{groupId}/sign")]
        [SwaggerOperation(OperationId = "SigningRequest")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<X509Certificate2ApiModel> PostSignAsync(string groupId, [FromBody] SigningRequestApiModel sr)
        {
            var onBehalfOfCertificateGroups = await this.certificateGroups.OnBehalfOfRequest(Request);
            return new X509Certificate2ApiModel(
                await onBehalfOfCertificateGroups.SigningRequestAsync(
                    groupId,
                    sr.ApplicationURI,
                    sr.ToServiceModel()));
        }

        /// <summary>New Key Pair</summary>
        [HttpPost("{groupId}/newkey")]
        [SwaggerOperation(OperationId = "NewKeyPairRequest")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<CertificateKeyPairApiModel> PostNewKeyAsync(string groupId, [FromBody] NewKeyPairRequestApiModel nkpr)
        {
            var onBehalfOfCertificateGroups = await this.certificateGroups.OnBehalfOfRequest(Request);
            return new CertificateKeyPairApiModel(
                await onBehalfOfCertificateGroups.NewKeyPairRequestAsync(
                    groupId,
                    nkpr.ApplicationURI,
                    nkpr.SubjectName,
                    nkpr.DomainNames,
                    nkpr.PrivateKeyFormat,
                    nkpr.PrivateKeyPassword));
        }
#endif
    }
}
