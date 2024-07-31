// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Filters;
    using Azure.IIoT.OpcUa.Publisher.Models;
    using Asp.Versioning;
    using Furly;
    using Furly.Extensions.AspNetCore.OpenApi;
    using Furly.Extensions.Http;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// CRUD and Query application resources
    /// </summary>
    [ApiVersion("2")]
    [Route("registry/v{version:apiVersion}/applications")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanRead)]
    [ApiController]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class ApplicationsController : ControllerBase
    {
        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="applications"></param>
        /// <param name="onboarding"></param>
        public ApplicationsController(IApplicationRegistry applications,
            INetworkDiscovery<string> onboarding)
        {
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
        /// <param name="discovererId">Scope the registration to a specific
        /// OPC Publisher using the publisher id</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="404">The publisher specified was not found.</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task RegisterServerAsync(
            [FromBody][Required] ServerRegistrationRequestModel request,
            [FromQuery] string? discovererId, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _onboarding.RegisterAsync(request, discovererId, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Disable an enabled application.
        /// </summary>
        /// <remarks>
        /// A manager can disable an application.
        /// </remarks>
        /// <param name="applicationId">The application id</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("{applicationId}/disable")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task DisableApplicationAsync(string applicationId,
            CancellationToken ct)
        {
            await _applications.DisableApplicationAsync(applicationId,
                ct: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Re-enable a disabled application.
        /// </summary>
        /// <remarks>
        /// A manager can enable an application.
        /// </remarks>
        /// <param name="applicationId">The application id</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("{applicationId}/enable")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task EnableApplicationAsync(string applicationId,
            CancellationToken ct)
        {
            await _applications.EnableApplicationAsync(applicationId,
                ct: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Discover servers
        /// </summary>
        /// <remarks>
        /// Registers servers by running a discovery scan in a supervisor's
        /// network. Requires that the onboarding agent service is running.
        /// </remarks>
        /// <param name="request">Discovery request</param>
        /// <param name="discovererId">Scope the discovery to a specific
        /// OPC Publisher using the publisher id</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="404">The publisher specified was not found.</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("discover")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task DiscoverServerAsync([FromBody][Required] DiscoveryRequestModel request,
            [FromQuery] string? discovererId, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            await _onboarding.DiscoverAsync(request, discovererId, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Cancel discovery
        /// </summary>
        /// <remarks>
        /// Cancels a discovery request using the request identifier.
        /// </remarks>
        /// <param name="requestId">Discovery request</param>
        /// <param name="discovererId">Scope the cancellation to a specific
        /// OPC Publisher using the publisher id</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="404">The publisher specified was not found.</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpDelete("discover/{requestId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task CancelAsync(string requestId, [FromQuery] string? discovererId,
            CancellationToken ct)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                throw new ArgumentNullException(nameof(requestId));
            }
            await _onboarding.CancelAsync(new DiscoveryCancelRequestModel
            {
                Id = requestId
                // TODO: AuthorityId = User.Identity.Name;
            }, discovererId, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Create new application
        /// </summary>
        /// <remarks>
        /// The application is registered using the provided information, but it
        /// is not associated with a publisher. This is useful for when you need
        /// to register clients or you want to register a server that is located
        /// in a network not reachable through a publisher module.
        /// </remarks>
        /// <param name="request">Application registration request</param>
        /// <param name="ct"></param>
        /// <returns>Application registration response</returns>
        /// <exception cref="ArgumentNullException"><paramref name="request"/> is
        /// <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPut]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task<ApplicationRegistrationResponseModel> CreateApplicationAsync(
            [FromBody][Required] ApplicationRegistrationRequestModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            var model = request;
            // TODO: model.AuthorityId = User.Identity.Name;
            return await _applications.RegisterApplicationAsync(model,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get application registration
        /// </summary>
        /// <param name="applicationId">Application id for the server</param>
        /// <param name="ct"></param>
        /// <returns>Application registration</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet("{applicationId}")]
        public async Task<ApplicationRegistrationModel> GetApplicationRegistrationAsync(
            string applicationId, CancellationToken ct)
        {
            return await _applications.GetApplicationAsync(applicationId,
                ct: ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <exception cref="ArgumentNullException"><paramref name="request"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPatch("{applicationId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task UpdateApplicationRegistrationAsync(string applicationId,
            [FromBody][Required] ApplicationRegistrationUpdateModel request,
            CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(request);
            var model = request;
            // TODO: applicationServiceModel.AuthorityId = User.Identity.Name;
            await _applications.UpdateApplicationAsync(applicationId, model,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Unregister application
        /// </summary>
        /// <remarks>
        /// Unregisters and deletes application and all its associated endpoints.
        /// </remarks>
        /// <param name="applicationId">The identifier of the application</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpDelete("{applicationId}")]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task DeleteApplicationAsync(string applicationId, CancellationToken ct)
        {
            await _applications.UnregisterApplicationAsync(applicationId, ct: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Purge applications
        /// </summary>
        /// <remarks>
        /// Purges all applications that have not been seen for a specified amount of time.
        /// </remarks>
        /// <param name="notSeenFor">A duration in milliseconds</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpDelete]
        [Authorize(Policy = Policies.CanWrite)]
        public async Task DeleteAllDisabledApplicationsAsync(
            [FromQuery] TimeSpan? notSeenFor, CancellationToken ct)
        {
            await _applications.PurgeDisabledApplicationsAsync(
                notSeenFor ?? TimeSpan.FromTicks(0), ct: ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Get list of sites
        /// </summary>
        /// <remarks>
        /// List all sites applications are registered in.
        /// </remarks>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <param name="ct"></param>
        /// <returns>Sites</returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet("sites")]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<ApplicationSiteListModel> GetListOfSitesAsync(
            [FromQuery] string? continuationToken,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            if (Request.Headers.TryGetValue(HttpHeader.ContinuationToken, out var value))
            {
                continuationToken = value.FirstOrDefault();
            }
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _applications.ListSitesAsync(continuationToken,
                pageSize, ct).ConfigureAwait(false);
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
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <param name="ct"></param>
        /// <returns>
        /// List of servers and continuation token to use for next request
        /// in x-ms-continuation header.
        /// </returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<ApplicationInfoListModel> GetListOfApplicationsAsync(
            [FromQuery] string? continuationToken,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            if (Request.Headers.TryGetValue(HttpHeader.ContinuationToken, out var value))
            {
                continuationToken = value.FirstOrDefault();
            }
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _applications.ListApplicationsAsync(continuationToken,
                pageSize, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns>Applications</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPost("query")]
        public async Task<ApplicationInfoListModel> QueryApplicationsAsync(
            [FromBody][Required] ApplicationRegistrationQueryModel query,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _applications.QueryApplicationsAsync(query,
                pageSize, ct).ConfigureAwait(false);
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
        /// <param name="ct"></param>
        /// <returns>Applications</returns>
        /// <exception cref="ArgumentNullException"><paramref name="query"/>
        /// is <c>null</c>.</exception>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpGet("query")]
        public async Task<ApplicationInfoListModel> GetFilteredListOfApplicationsAsync(
            [FromBody][Required] ApplicationRegistrationQueryModel query,
            [FromQuery] int? pageSize, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(query);
            if (Request.Headers.TryGetValue(HttpHeader.MaxItemCount, out var value))
            {
                pageSize = int.Parse(value.FirstOrDefault()!,
                    CultureInfo.InvariantCulture);
            }
            return await _applications.QueryApplicationsAsync(query,
                pageSize, ct).ConfigureAwait(false);
        }

        private readonly IApplicationRegistry _applications;
        private readonly INetworkDiscovery<string> _onboarding;
    }
}
