// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;
    using Swashbuckle.AspNetCore.Swagger;

    /// <summary>
    /// CRUD and Query application resources
    /// </summary>
    [Route(VersionInfo.PATH + "/applications")]
    [ExceptionsFilter]
    [Produces(ContentEncodings.MimeTypeJson)]
    [Authorize(Policy = Policies.CanQuery)]
    public class ApplicationsController : Controller {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="applications"></param>
        /// <param name="query"></param>
        /// <param name="onboarding"></param>
        public ApplicationsController(IApplicationRegistry applications,
            IApplicationRecordQuery query, IOnboardingServices onboarding) {
            _query = query;
            _applications = applications;
            _onboarding = onboarding;
        }

        /// <summary>
        /// Register new server
        /// </summary>
        /// <remarks>
        /// Registers a server solely using a discovery url. Requires that
        /// the onboarding agent service is running and the server can be
        /// located by a supervisor in its network using the discovery url.
        /// </remarks>
        /// <param name="request">Server registration request</param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Policy = Policies.CanManage)]
        public async Task RegisterServerAsync(
            [FromBody] [Required] ServerRegistrationRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _onboarding.RegisterAsync(request.ToServiceModel());
        }

        /// <summary>
        /// Disable an enabled application.
        /// </summary>
        /// <remarks>
        /// A manager can disable an application.
        /// </remarks>
        /// <param name="applicationId">The application id</param>
        /// <returns></returns>
        [HttpPost("{applicationId}/disable")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DisableApplicationAsync(string applicationId) {
            await _applications.DisableApplicationAsync(applicationId);
        }

        /// <summary>
        /// Re-enable a disabled application.
        /// </summary>
        /// <remarks>
        /// A manager can enable an application.
        /// </remarks>
        /// <param name="applicationId">The application id</param>
        /// <returns></returns>
        [HttpPost("{applicationId}/enable")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task EnableApplicationAsync(string applicationId) {
            await _applications.EnableApplicationAsync(applicationId);
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        /// <remarks>
        /// Registers servers by running a discovery scan in a supervisor's
        /// network. Requires that the onboarding agent service is running.
        /// </remarks>
        /// <param name="request">Discovery request</param>
        /// <returns></returns>
        [HttpPost("discover")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DiscoverServerAsync(
            [FromBody] [Required] DiscoveryRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            await _onboarding.DiscoverAsync(request.ToServiceModel());
        }

        /// <summary>
        /// Create new application
        /// </summary>
        /// <remarks>
        /// The application is registered using the provided information, but it
        /// is not associated with a supervisor.  This is useful for when you need
        /// to register clients or you want to register a server that is located
        /// in a network not reachable through a Twin module.
        /// </remarks>
        /// <param name="request">Application registration request</param>
        /// <returns>Application registration response</returns>
        [HttpPut]
        [Authorize(Policy = Policies.CanManage)]
        public async Task<ApplicationRegistrationResponseApiModel> CreateApplicationAsync(
            [FromBody] [Required] ApplicationRegistrationRequestApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var model = request.ToServiceModel();
            // TODO: applicationServiceModel.AuthorityId = User.Identity.Name;
            var result = await _applications.RegisterApplicationAsync(model);
            return new ApplicationRegistrationResponseApiModel(result);
        }

        /// <summary>
        /// Get application registration
        /// </summary>
        /// <param name="applicationId">Application id for the server</param>
        /// <returns>Application registration</returns>
        [HttpGet("{applicationId}")]
        public async Task<ApplicationRegistrationApiModel> GetApplicationRegistrationAsync(
            string applicationId) {
            var result = await _applications.GetApplicationAsync(applicationId);
            return new ApplicationRegistrationApiModel(result);
        }

        /// <summary>
        /// Update application registration
        /// </summary>
        /// <remarks>
        /// The application information is updated with new properties.  Note that
        /// this information might be overridden if the application is re-discovered
        /// during a discovery run (recurring or one-time).
        /// </remarks>
        /// <param name="applicationId">The identifier of the application</param>
        /// <param name="request">Application update request</param>
        [HttpPatch("{applicationId}")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task UpdateApplicationRegistrationAsync(string applicationId,
            [FromBody] [Required] ApplicationRegistrationUpdateApiModel request) {
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var model = request.ToServiceModel();
            // TODO: applicationServiceModel.AuthorityId = User.Identity.Name;
            await _applications.UpdateApplicationAsync(applicationId, model);
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        /// <remarks>
        /// Unregisters and deletes application and all its associated endpoints.
        /// </remarks>
        /// <param name="applicationId">The identifier of the application</param>
        /// <returns></returns>
        [HttpDelete("{applicationId}")]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DeleteApplicationAsync(string applicationId) {
            await _applications.UnregisterApplicationAsync(applicationId);
        }

        /// <summary>
        /// Purge applications
        /// </summary>
        /// <remarks>
        /// Purges all applications that have not been seen for a specified amount of time.
        /// </remarks>
        /// <param name="notSeenFor">A duration in milliseconds</param>
        /// <returns></returns>
        [HttpDelete]
        [Authorize(Policy = Policies.CanManage)]
        public async Task DeleteAllDisabledApplicationsAsync(
            [FromQuery] TimeSpan? notSeenFor) {
            await _applications.PurgeDisabledApplicationsAsync(
                notSeenFor ?? TimeSpan.FromTicks(0));
        }

        /// <summary>
        /// Get list of sites
        /// </summary>
        /// <remarks>
        /// List all sites applications are registered in.
        /// </remarks>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <returns>Sites</returns>
        [HttpGet("sites")]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<ApplicationSiteListApiModel> GetListOfSitesAsync(
            [FromQuery] string continuationToken,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _applications.ListSitesAsync(
                continuationToken, pageSize);
            return new ApplicationSiteListApiModel(result);
        }

        /// <summary>
        /// Get list of applications
        /// </summary>
        /// <remarks>
        /// Get all registered applications in paged form.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call this operation again using the token to retrieve more results.
        /// </remarks>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <returns>
        /// List of servers and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<ApplicationInfoListApiModel> GetListOfApplicationsAsync(
            [FromQuery] string continuationToken,
            [FromQuery] int? pageSize) {
            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _applications.ListApplicationsAsync(
                continuationToken, pageSize);

            return new ApplicationInfoListApiModel(result);
        }

        /// <summary>
        /// Query applications
        /// </summary>
        /// <remarks>
        /// List applications that match a query model.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfApplications operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Application query</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Applications</returns>
        [HttpPost("query")]
        public async Task<ApplicationInfoListApiModel> QueryApplicationsAsync(
            [FromBody] [Required] ApplicationRegistrationQueryApiModel query,
            [FromQuery] int? pageSize) {

            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _applications.QueryApplicationsAsync(
                query.ToServiceModel(), pageSize);

            return new ApplicationInfoListApiModel(result);
        }

        /// <summary>
        /// Query applications by id.
        /// </summary>
        /// <remarks>
        /// A query model which supports the OPC UA Global Discovery Server query.
        /// </remarks>
        /// <param name="query"></param>
        /// <returns></returns>
        [HttpPost("querybyid")]
        public async Task<ApplicationRecordListApiModel> QueryApplicationsByIdAsync(
            [FromBody] ApplicationRecordQueryApiModel query) {
            if (query == null) {
                // query all
                query = new ApplicationRecordQueryApiModel();
            }
            var result = await _query.QueryApplicationsAsync(
                query.ToServiceModel());
            return new ApplicationRecordListApiModel(result);
        }

        /// <summary>
        /// Get filtered list of applications
        /// </summary>
        /// <remarks>
        /// Get a list of applications filtered using the specified query parameters.
        /// The returned model can contain a continuation token if more results are
        /// available.
        /// Call the GetListOfApplications operation using the token to retrieve
        /// more results.
        /// </remarks>
        /// <param name="query">Applications Query model</param>
        /// <param name="pageSize">Number of results to return</param>
        /// <returns>Applications</returns>
        [HttpGet("query")]
        public async Task<ApplicationInfoListApiModel> GetFilteredListOfApplicationsAsync(
            [FromBody] [Required] ApplicationRegistrationQueryApiModel query,
            [FromQuery] int? pageSize) {

            if (query == null) {
                throw new ArgumentNullException(nameof(query));
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _applications.QueryApplicationsAsync(
                query.ToServiceModel(), pageSize);

            return new ApplicationInfoListApiModel(result);
        }

        private readonly IApplicationRecordQuery _query;
        private readonly IApplicationRegistry _applications;
        private readonly IOnboardingServices _onboarding;
    }
}
