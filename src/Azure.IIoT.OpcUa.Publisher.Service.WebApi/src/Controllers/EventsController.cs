// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Service.WebApi.Controllers
{
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.Filters;
    using Azure.IIoT.OpcUa.Publisher.Service.WebApi.SignalR;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading;
    using System.Threading.Tasks;
    using Furly;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// Configure discovery events
    /// </summary>
    [ApiVersion("2")]
    [Route("events/v{version:apiVersion}")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanWrite)]
    [ApiController]
    [Produces(ContentMimeType.Json, ContentMimeType.MsgPack)]
    [Consumes(ContentMimeType.Json, ContentMimeType.MsgPack)]
    public class EventsController : ControllerBase
    {
        /// <summary>
        /// Create controller for discovery services
        /// </summary>
        /// <param name="events"></param>
        public EventsController(IGroupRegistration<DiscoverersHub> events)
        {
            _events = events;
        }

        /// <summary>
        /// Subscribe to discovery progress from discoverer
        /// </summary>
        /// <remarks>
        /// Register a client to receive discovery progress events
        /// through SignalR from a particular discoverer.
        /// </remarks>
        /// <param name="discovererId">The discoverer to subscribe to</param>
        /// <param name="connectionId">The connection that will receive discovery
        /// events.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPut("discovery/{discovererId}/events")]
        public async Task SubscribeByDiscovererIdAsync(string discovererId,
            [FromBody] string connectionId, CancellationToken ct)
        {
            await _events.SubscribeAsync(discovererId, connectionId,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Subscribe to discovery progress for a request
        /// </summary>
        /// <remarks>
        /// Register a client to receive discovery progress events
        /// through SignalR for a particular request.
        /// </remarks>
        /// <param name="requestId">The request to monitor</param>
        /// <param name="connectionId">The connection that will receive discovery
        /// events.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpPut("discovery/requests/{requestId}/events")]
        public async Task SubscribeByRequestIdAsync(string requestId,
            [FromBody] string connectionId, CancellationToken ct)
        {
            await _events.SubscribeAsync(requestId, connectionId,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Unsubscribe from discovery progress for a request.
        /// </summary>
        /// <remarks>
        /// Unregister a client and stop it from receiving discovery
        /// events for a particular request.
        /// </remarks>
        /// <param name="requestId">The request to unsubscribe from
        /// </param>
        /// <param name="connectionId">The connection that will not receive
        /// any more discovery progress</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpDelete("discovery/requests/{requestId}/events/{connectionId}")]
        public async Task UnsubscribeByRequestIdAsync(string requestId,
            string connectionId, CancellationToken ct)
        {
            await _events.UnsubscribeAsync(requestId, connectionId,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Unsubscribe from discovery progress from discoverer.
        /// </summary>
        /// <remarks>
        /// Unregister a client and stop it from receiving discovery events.
        /// </remarks>
        /// <param name="discovererId">The discoverer to unsubscribe from
        /// </param>
        /// <param name="connectionId">The connection that will not receive
        /// any more discovery progress</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <response code="200">The operation was successful.</response>
        /// <response code="400">The passed in information is invalid</response>
        /// <response code="500">An internal error ocurred.</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [HttpDelete("discovery/{discovererId}/events/{connectionId}")]
        public async Task UnsubscribeByDiscovererIdAsync(string discovererId,
            string connectionId, CancellationToken ct)
        {
            await _events.UnsubscribeAsync(discovererId, connectionId,
                ct).ConfigureAwait(false);
        }

        private readonly IGroupRegistration<DiscoverersHub> _events;
    }
}
