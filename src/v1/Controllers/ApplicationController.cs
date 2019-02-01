// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Auth;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Filters;
using Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Models;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    /// <inheritdoc/>
    [ApiController]
    [Route(VersionInfo.PATH + "/app"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]
    [Authorize(Policy = Policies.CanRead)]
    public sealed class ApplicationController : Controller
    {
        private readonly IApplicationsDatabase _applicationDatabase;

        /// <inheritdoc/>
        public ApplicationController(IApplicationsDatabase applicationDatabase)
        {
            _applicationDatabase = applicationDatabase;
        }

        /// <summary>
        /// Register new application.
        /// </summary>
        [HttpPost("register")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<ApplicationRecordApiModel> RegisterApplicationAsync([FromBody] ApplicationRecordApiModel application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            var applicationServiceModel = application.ToServiceModel();
            applicationServiceModel.AuthorityId = User.Identity.Name;
            return new ApplicationRecordApiModel(await _applicationDatabase.RegisterApplicationAsync(applicationServiceModel));
        }

        /// <summary>
        /// Get application.
        /// </summary>
        [HttpGet("{applicationId}")]
        public async Task<ApplicationRecordApiModel> GetApplicationAsync(string applicationId)
        {
            return new ApplicationRecordApiModel(await _applicationDatabase.GetApplicationAsync(applicationId));
        }

        /// <summary>
        /// Update application.
        /// </summary>
        [HttpPut("{applicationId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<ApplicationRecordApiModel> UpdateApplicationAsync([FromBody] ApplicationRecordApiModel application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            var applicationServiceModel = application.ToServiceModel();
            applicationServiceModel.AuthorityId = User.Identity.Name;
            return new ApplicationRecordApiModel(await _applicationDatabase.UpdateApplicationAsync(application.ApplicationId, applicationServiceModel));
        }

        /// <summary>
        /// Approve or reject new application.
        /// </summary>
        [HttpPost("{applicationId}/{approved}/approve")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<ApplicationRecordApiModel> ApproveApplicationAsync(string applicationId, bool approved, bool? force)
        {
            return new ApplicationRecordApiModel(await _applicationDatabase.ApproveApplicationAsync(applicationId, approved, force ?? false));
        }

        /// <summary>
        /// Unregister application.
        /// </summary>
        [HttpPost("{applicationId}/unregister")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<ApplicationRecordApiModel> UnregisterApplicationAsync(string applicationId)
        {
            return new ApplicationRecordApiModel(await _applicationDatabase.UnregisterApplicationAsync(applicationId));
        }

        /// <summary>
        /// Delete application.
        /// </summary>
        [HttpDelete("{applicationId}")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DeleteApplicationAsync(string applicationId, bool? force)
        {
            await _applicationDatabase.DeleteApplicationAsync(applicationId, force ?? false);
        }

        /// <summary>Find applications</summary>
        [HttpGet("find/{uri}")]
        public async Task<IList<ApplicationRecordApiModel>> ListApplicationsAsync(string uri)
        {
            var modelResult = new List<ApplicationRecordApiModel>();
            foreach (var record in await _applicationDatabase.ListApplicationAsync(uri))
            {
                modelResult.Add(new ApplicationRecordApiModel(record));
            }
            return modelResult;
        }

        /// <summary>Query applications</summary>
        [HttpPost("query")]
        public async Task<QueryApplicationsResponseApiModel> QueryApplicationsAsync([FromBody] QueryApplicationsApiModel query, bool? anyState)
        {
            if (query == null)
            {
                // query all
                query = new QueryApplicationsApiModel(0, 0, null, null, 0, null, null);
            }
            var result = await _applicationDatabase.QueryApplicationsAsync(
                query.StartingRecordId,
                query.MaxRecordsToReturn,
                query.ApplicationName,
                query.ApplicationUri,
                query.ApplicationType,
                query.ProductUri,
                query.ServerCapabilities,
                anyState
                );
            return new QueryApplicationsResponseApiModel(result);
        }

        /// <summary>Query applications</summary>
        [HttpPost("query/page")]
        //[AutoRestExtension(ContinuationTokenLinkName = "nextPageLink")]
        public async Task<QueryApplicationsPageResponseApiModel> QueryApplicationsPageAsync(
            [FromBody] QueryApplicationsPageApiModel query,
            bool? anyState)
        {
            if (query == null)
            {
                // query all
                query = new QueryApplicationsPageApiModel(null, null, 0, null, null);
            }
            var result = await _applicationDatabase.QueryApplicationsPageAsync(
                query.ApplicationName,
                query.ApplicationUri,
                query.ApplicationType,
                query.ProductUri,
                query.ServerCapabilities,
                query.NextPageLink,
                query.MaxRecordsToReturn,
                anyState);
            return new QueryApplicationsPageResponseApiModel(result);
        }

    }
}
