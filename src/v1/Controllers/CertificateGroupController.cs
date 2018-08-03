// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Filters;
using Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.GdsVault.v1.Controllers
{
    [Route(VersionInfo.PATH + "/groups"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]

    public sealed class CertificateGroupController : Controller
    {
        private readonly ICertificateGroup certificateGroups;

        public CertificateGroupController(ICertificateGroup certificateGroups)
        {
            this.certificateGroups = certificateGroups;
        }

        /// <returns>List of certificate groups</returns>
        [HttpGet]
        [SwaggerOperation(operationId: "GetCertificateGroupIds")]
        public async Task<CertificateGroupListApiModel> GetAsync()
        {
            return new CertificateGroupListApiModel(await this.certificateGroups.GetCertificateGroupIds());
        }

        /// <summary>Get group configuration</summary>
        [HttpGet("{groupId}")]
        [SwaggerOperation(operationId: "GetCertificateGroupConfiguration")]
        public async Task<CertificateGroupConfigurationApiModel> GetAsync(string groupId)
        {
            return new CertificateGroupConfigurationApiModel(
                groupId,
                await this.certificateGroups.GetCertificateGroupConfiguration(groupId));
        }

        /// <summary>Get group configuration</summary>
        [HttpGet("config")]
        [SwaggerOperation(operationId: "GetCertificateGroupConfigurationCollection")]
        public async Task<CertificateGroupConfigurationCollectionApiModel> GetConfigAsync()
        {
            return new CertificateGroupConfigurationCollectionApiModel(
                await this.certificateGroups.GetCertificateGroupConfigurationCollection());
        }

        /// <summary>Get CA Certificate chain</summary>
        [HttpGet("{groupId}/cacert")]
        [SwaggerOperation(operationId: "GetCACertificateChain")]
        public async Task<X509Certificate2CollectionApiModel> GetCACertificateChainAsync(string groupId)
        {
            return new X509Certificate2CollectionApiModel(
                await this.certificateGroups.GetCACertificateChainAsync(groupId));
        }

        /// <summary>Get CA CRL chain</summary>
        [HttpGet("{groupId}/cacrl")]
        [SwaggerOperation(operationId: "GetCACrlChain")]
        public async Task<X509CrlCollectionApiModel> GetCACrlChainAsync(string groupId)
        {
            return new X509CrlCollectionApiModel(
                await this.certificateGroups.GetCACrlChainAsync(groupId));
        }

        /// <summary>Get trust list</summary>
        [HttpGet("{groupId}/trustlist")]
        [SwaggerOperation(operationId: "GetTrustList")]
        public async Task<TrustListApiModel> GetTrustListAsync(string groupId)
        {
            return new TrustListApiModel(await this.certificateGroups.GetTrustListAsync(groupId));
        }

        /// <summary>Create new CA Certificate</summary>
        [HttpPost("{groupId}/create")]
        [SwaggerOperation(operationId: "CreateCACertificate")]
        public async Task<X509Certificate2ApiModel> PostCreateAsync(string groupId)
        {
            return new X509Certificate2ApiModel(
                await this.certificateGroups.CreateCACertificateAsync(groupId));
        }

#if CERTSIGNER
        /// <summary>Revoke Certificate</summary>
        [HttpPost("{groupId}/revoke")]
        [SwaggerOperation(operationId: "RevokeCertificate")]
        public async Task<X509CrlApiModel> PostRevokeAsync(string groupId, [FromBody] X509Certificate2ApiModel cert)
        {
            return new X509CrlApiModel(
                await this.certificateGroups.RevokeCertificateAsync(
                    groupId,
                    cert.ToServiceModel()));
        }

        /// <summary>Signing Request</summary>
        [HttpPost("{groupId}/sign")]
        [SwaggerOperation(operationId: "SigningRequest")]
        public async Task<X509Certificate2ApiModel> PostSignAsync(string groupId, [FromBody] SigningRequestApiModel sr)
        {
            return new X509Certificate2ApiModel(
                await this.certificateGroups.SigningRequestAsync(
                    groupId,
                    sr.ApplicationURI,
                    sr.ToServiceModel()));
        }

        /// <summary>New Key Pair</summary>
        [HttpPost("{groupId}/newkey")]
        [SwaggerOperation(operationId: "NewKeyPairRequest")]
        public async Task<CertificateKeyPairApiModel> PostNewKeyAsync(string groupId, [FromBody] NewKeyPairRequestApiModel nkpr)
        {
            return new CertificateKeyPairApiModel(
                await this.certificateGroups.NewKeyPairRequestAsync(
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
