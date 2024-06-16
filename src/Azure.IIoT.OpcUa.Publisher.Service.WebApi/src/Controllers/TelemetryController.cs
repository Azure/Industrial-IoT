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

    /// <summary>
    /// Value and Event monitoring services
    /// </summary>
    [ApiVersion("2")]
    [Route("events/v{version:apiVersion}")]
    [ExceptionsFilter]
    [Authorize(Policy = Policies.CanWrite)]
    [ApiController]
    public class TelemetryController : ControllerBase
    {
        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="events"></param>
        public TelemetryController(IGroupRegistration<PublishersHub> events)
        {
            _events = events;
        }

        /// <summary>
        /// Subscribe to receive samples
        /// </summary>
        /// <remarks>
        /// Register a client to receive publisher samples through SignalR.
        /// </remarks>
        /// <param name="endpointId">The endpoint to subscribe to</param>
        /// <param name="connectionId">The connection that will receive publisher
        /// samples.</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPut("telemetry/{endpointId}/samples")]
        public async Task SubscribeAsync(string endpointId,
            [FromBody] string connectionId, CancellationToken ct)
        {
            await _events.SubscribeAsync(endpointId, connectionId,
                ct).ConfigureAwait(false);
        }

        /// <summary>
        /// Unsubscribe from receiving samples.
        /// </summary>
        /// <remarks>
        /// Unregister a client and stop it from receiving samples.
        /// </remarks>
        /// <param name="endpointId">The endpoint to unsubscribe from
        /// </param>
        /// <param name="connectionId">The connection that will not receive
        /// any more published samples</param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpDelete("telemetry/{endpointId}/samples/{connectionId}")]
        public async Task UnsubscribeAsync(string endpointId, string connectionId,
            CancellationToken ct)
        {
            await _events.UnsubscribeAsync(endpointId, connectionId,
                ct).ConfigureAwait(false);
        }

        private readonly IGroupRegistration<PublishersHub> _events;
    }
}
