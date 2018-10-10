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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.IIoT.OpcUa.Services.Vault.v1.Controllers
{
    /// <inheritdoc/>
    [Route(VersionInfo.PATH + "/app"), TypeFilter(typeof(ExceptionsFilterAttribute))]
    [Produces("application/json")]
    [Authorize(Policy = Policies.CanRead)]
    public sealed class ApplicationController : Controller
    {
        private readonly IApplicationsDatabase _applicationDatabase;

        /// <inheritdoc/>
        public ApplicationController(IApplicationsDatabase applicationDatabase)
        {
            this._applicationDatabase = applicationDatabase;
        }

        /// <summary>
        /// Register new application.
        /// </summary>
        [HttpPost]
        [SwaggerOperation(OperationId = "RegisterApplication")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<string> RegisterApplicationAsync([FromBody] ApplicationRecordApiModel application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            return await _applicationDatabase.RegisterApplicationAsync(application.ToServiceModel());
        }

        /// <summary>
        /// Update application.
        /// </summary>
        [HttpPut("{applicationId}")]
        [SwaggerOperation(OperationId = "UpdateApplication")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<string> UpdateApplicationAsync(string applicationId, [FromBody] ApplicationRecordApiModel application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }
            return await _applicationDatabase.UpdateApplicationAsync(applicationId, application.ToServiceModel());
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        [HttpDelete("{applicationId}")]
        [SwaggerOperation(OperationId = "UnregisterApplication")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task UnregisterApplicationAsync(string applicationId)
        {
            await _applicationDatabase.UnregisterApplicationAsync(applicationId);
        }

        /// <summary>Get application</summary>
        [HttpGet("{applicationId}")]
        [SwaggerOperation(OperationId = "GetApplication")]
        public async Task<ApplicationRecordApiModel> GetApplicationAsync(string applicationId)
        {
            return new ApplicationRecordApiModel(await _applicationDatabase.GetApplicationAsync(applicationId));
        }

        /// <summary>Find applications</summary>
        [HttpGet("find/{uri}")]
        [SwaggerOperation(OperationId = "FindApplication")]
        public async Task<ApplicationRecordApiModel[]> FindApplicationAsync(string uri)
        {
            var modelResult = new List<ApplicationRecordApiModel>();
            foreach (var record in await _applicationDatabase.FindApplicationAsync(uri))
            {
                modelResult.Add(new ApplicationRecordApiModel(record));
            }
            return modelResult.ToArray();
        }

        /// <summary>Query applications</summary>
        [HttpPost("query")]
        [SwaggerOperation(OperationId = "QueryApplications")]
        public async Task<QueryApplicationsResponseApiModel> QueryApplicationsAsync([FromBody] QueryApplicationsApiModel query)
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
                query.ServerCapabilities
                );
            return new QueryApplicationsResponseApiModel(result);
        }
    }
}
