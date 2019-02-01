// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.Exceptions;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Filters;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    /// <inheritdoc/>
    [ApiController]
    [Route(VersionInfo.PATH + "/group"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]
    [Authorize(Policy = Policies.CanRead)]
    public sealed class CertificateGroupController : Controller
    {
        private readonly ICertificateGroup _certificateGroups;

        /// <inheritdoc/>
        public CertificateGroupController(
            ICertificateGroup certificateGroups)
        {
            this._certificateGroups = certificateGroups;
        }

        /// <returns>List of certificate groups</returns>
        [HttpGet]
        public async Task<CertificateGroupListApiModel> GetCertificateGroupsAsync()
        {
            return new CertificateGroupListApiModel(await this._certificateGroups.GetCertificateGroupIds());
        }

        /// <summary>Get group configuration</summary>
        [HttpGet("{group}")]
        public async Task<CertificateGroupConfigurationApiModel> GetCertificateGroupConfigurationAsync(string group)
        {
            return new CertificateGroupConfigurationApiModel(
                group,
                await this._certificateGroups.GetCertificateGroupConfiguration(group));
        }

        /// <summary>Update group configuration</summary>
        [HttpPut("{group}")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<CertificateGroupConfigurationApiModel> UpdateCertificateGroupConfigurationAsync(string group, [FromBody] CertificateGroupConfigurationApiModel config)
        {
            var onBehalfOfCertificateGroups = await this._certificateGroups.OnBehalfOfRequest(Request);
            return new CertificateGroupConfigurationApiModel(
                group,
                await onBehalfOfCertificateGroups.UpdateCertificateGroupConfiguration(group, config.ToServiceModel()));
        }

        /// <summary>Create new group configuration</summary>
        [HttpPost("{group}/{subject}/{certType}/create")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<CertificateGroupConfigurationApiModel> CreateCertificateGroupAsync(string group, string subject, string certType)
        {
            var onBehalfOfCertificateGroups = await this._certificateGroups.OnBehalfOfRequest(Request);
            return new CertificateGroupConfigurationApiModel(
                group,
                await onBehalfOfCertificateGroups.CreateCertificateGroupConfiguration(group, subject, certType));
        }

        /// <summary>Delete group configuration</summary>
        [HttpDelete("{group}")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DeleteCertificateGroupAsync(string group)
        {
            await Task.Delay(1000);
            // intentionally not implemented yet
            throw new ResourceNotFoundException();
        }

        /// <summary>Get group configuration</summary>
        [HttpGet("groupsconfig")]
        public async Task<CertificateGroupConfigurationCollectionApiModel> GetCertificateGroupsConfigurationAsync()
        {
            return new CertificateGroupConfigurationCollectionApiModel(
                await this._certificateGroups.GetCertificateGroupConfigurationCollection());
        }

        /// <summary>Get Issuer CA Certificate chain</summary>
        [HttpGet("{group}/issuerca")]
        public async Task<X509Certificate2CollectionApiModel> GetCertificateGroupIssuerCAChainAsync(string group, int? maxResults)
        {
            return new X509Certificate2CollectionApiModel(
                await this._certificateGroups.GetIssuerCACertificateChainAsync(group));
        }

        /// <summary>Get Issuer CA Certificate chain</summary>
        [HttpPost("{group}/issuerca/next")]
        public async Task<X509Certificate2CollectionApiModel> GetCertificateGroupIssuerCAChainNextAsync(string group, [FromBody] string nextPageLink, int? maxResults)
        {
            return new X509Certificate2CollectionApiModel(
                await this._certificateGroups.GetIssuerCACertificateChainAsync(group));
        }

        /// <summary>Get Issuer CA CRL chain</summary>
        [HttpGet("{group}/issuercacrl")]
        public async Task<X509CrlCollectionApiModel> GetCertificateGroupIssuerCACrlChainAsync(string group, int? maxResults)
        {
            return new X509CrlCollectionApiModel(
                await this._certificateGroups.GetIssuerCACrlChainAsync(group));
        }

        /// <summary>Get Issuer CA CRL chain</summary>
        [HttpPost("{group}/issuercacrl/next")]
        public async Task<X509CrlCollectionApiModel> GetCertificateGroupIssuerCACrlChainNextAsync(string group, [FromBody] string nextPageLink, int? maxResults)
        {
            return new X509CrlCollectionApiModel(
                await this._certificateGroups.GetIssuerCACrlChainAsync(group));
        }

        /// <summary>Get trust list</summary>
        [HttpGet("{group}/trustlist")]
        public async Task<TrustListApiModel> GetCertificateGroupTrustListAsync(string group, int? maxResults)
        {
            return new TrustListApiModel(await this._certificateGroups.GetTrustListAsync(group, maxResults, null));
        }

        /// <summary>Get trust list</summary>
        [HttpPost("{group}/trustlist/next")]
        public async Task<TrustListApiModel> GetCertificateGroupTrustListNextAsync(string group, [FromBody] string nextPageLink, int? maxResults)
        {
            return new TrustListApiModel(await this._certificateGroups.GetTrustListAsync(group, maxResults, nextPageLink));
        }

        /// <summary>Create new CA Certificate</summary>
        [HttpPost("{group}/issuerca/create")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<X509Certificate2ApiModel> CreateCertificateGroupIssuerCACertAsync(string group)
        {
            var onBehalfOfCertificateGroups = await this._certificateGroups.OnBehalfOfRequest(Request);
            return new X509Certificate2ApiModel(
                await onBehalfOfCertificateGroups.CreateIssuerCACertificateAsync(group));
        }

    }
}
