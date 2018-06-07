// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.v1.Controllers {
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.v1.Auth;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.v1.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Registry.v1.Models;
    using Microsoft.Azure.IIoT.OpcUa.Services;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Applications controller
    /// </summary>
    [Route(VersionInfo.PATH + "/[controller]")]
    [ExceptionsFilter]
    [Produces("application/json")]
    public class ApplicationsController : Controller {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="applications"></param>
        public ApplicationsController(IOpcUaApplicationRegistry applications) {
            _applications = applications;
        }

        /// <summary>
        /// Register new server in the application registry
        /// </summary>
        /// <param name="request">Server registration request</param>
        /// <returns>Application registration response</returns>
        [HttpPost]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task<ApplicationRegistrationResponseApiModel> PostAsync(
            [FromBody] ServerRegistrationRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _applications.RegisterAsync(
                request.ToServiceModel());
            return new ApplicationRegistrationResponseApiModel(result);
        }

        /// <summary>
        /// Register new application in the application registry
        /// using raw information from application info.
        /// </summary>
        /// <param name="request">Application registration request</param>
        /// <returns>Application registration response</returns>
        [HttpPut]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task<ApplicationRegistrationResponseApiModel> PutAsync(
            [FromBody] ApplicationRegistrationRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var result = await _applications.RegisterAsync(
                request.ToServiceModel());
            return new ApplicationRegistrationResponseApiModel(result);
        }

        /// <summary>
        /// Update existing application. Note that Id field in request
        /// must not be null and application registration must exist.
        /// </summary>
        /// <param name="request">Twin update request</param>
        [HttpPatch]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task PatchAsync(
            [FromBody] ApplicationRegistrationUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _applications.UpdateApplicationAsync(request.ToServiceModel());
        }

        /// <summary>
        /// Delete application by application identifier.
        /// </summary>
        /// <param name="id">The identifier of the application</param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task DeleteAsync(string id) {
            await _applications.UnregisterApplicationAsync(id);
        }

        /// <summary>
        /// Purge all applications that have not been seen since the
        /// amount of time.
        /// </summary>
        /// <param name="notSeenFor">Older than this time in utc</param>
        /// <returns></returns>
        [HttpDelete]
        [Authorize(Policy = Policy.RegisterTwins)]
        public async Task PurgeAsync(
            [FromQuery] TimeSpan? notSeenFor) {
            await _applications.PurgeDisabledApplicationsAsync(
                notSeenFor ?? TimeSpan.FromTicks(0));
        }

        /// <summary>
        /// List all sites applications are registered in.
        /// </summary>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <returns>Sites</returns>
        [HttpGet("sites")]
        public async Task<ApplicationSiteListApiModel> GetSitesAsync(
            [FromQuery] string continuationToken,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(kContinuationTokenHeaderKey)) {
                continuationToken = Request.Headers[kContinuationTokenHeaderKey]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(kPageSizeHeaderKey)) {
                pageSize = int.Parse(Request.Headers[kPageSizeHeaderKey]
                    .FirstOrDefault());
            }
            var result = await _applications.ListSitesAsync(
                continuationToken, pageSize);
            return new ApplicationSiteListApiModel(result);
        }

        /// <summary>
        /// Get all registered applications in paged form.
        /// </summary>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <returns>
        /// List of servers and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        public async Task<ApplicationInfoListApiModel> ListAsync(
            [FromQuery] string continuationToken,
            [FromQuery] int? pageSize) {
            if (Request.Headers.ContainsKey(kContinuationTokenHeaderKey)) {
                continuationToken = Request.Headers[kContinuationTokenHeaderKey]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(kPageSizeHeaderKey)) {
                pageSize = int.Parse(Request.Headers[kPageSizeHeaderKey]
                    .FirstOrDefault());
            }
            var result = await _applications.ListApplicationsAsync(
                continuationToken, pageSize);

            // TODO: Filter results based on RBAC

            return new ApplicationInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the applications for the information in the
        /// specified application query info model.
        /// </summary>
        /// <param name="query">Application query</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Applications</returns>
        [HttpPost("query")]
        public async Task<ApplicationInfoListApiModel> FindAsync(
            [FromBody] ApplicationRegistrationQueryApiModel query,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(kPageSizeHeaderKey)) {
                pageSize = int.Parse(Request.Headers[kPageSizeHeaderKey]
                    .FirstOrDefault());
            }
            var result = await _applications.QueryApplicationsAsync(
                query.ToServiceModel(), pageSize);

            // TODO: Filter results based on RBAC

            return new ApplicationInfoListApiModel(result);
        }

        /// <summary>
        /// Query using Uri query specification.
        /// </summary>
        /// <param name="query">Application Query</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Applications</returns>
        [HttpGet("query")]
        public async Task<ApplicationInfoListApiModel> QueryAsync(
            [FromQuery] ApplicationRegistrationQueryApiModel query,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(kPageSizeHeaderKey)) {
                pageSize = int.Parse(Request.Headers[kPageSizeHeaderKey]
                    .FirstOrDefault());
            }
            var result = await _applications.QueryApplicationsAsync(
                query.ToServiceModel(), pageSize);

            // TODO: Filter results based on RBAC

            return new ApplicationInfoListApiModel(result);
        }

        /// <summary>
        /// Returns the application data for the application identified by the
        /// specified application identifier.
        /// </summary>
        /// <param name="id">Application id for the server</param>
        /// <returns>Application model</returns>
        [HttpGet("{id}")]
        public async Task<ApplicationRegistrationApiModel> GetAsync(string id) {
            var result = await _applications.GetApplicationAsync(id);

            // TODO: Filter results based on RBAC

            return new ApplicationRegistrationApiModel(result);
        }

        private const string kContinuationTokenHeaderKey = "x-ms-continuation";
        private const string kPageSizeHeaderKey = "x-ms-max-item-count";
        private readonly IOpcUaApplicationRegistry _applications;
    }
}