// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Publisher.v2.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Publisher.v2.Filters;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.IIoT.Messaging;
    using System.Threading.Tasks;

    /// <summary>
    /// Value and Event monitoring services
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/monitor")]
    [ExceptionsFilter]
    [Produces(ContentMimeType.Json)]
    [Authorize(Policy = Policies.CanPublish)]
    [ApiController]
    public class MonitorController : ControllerBase {

        /// <summary>
        /// Create controller with service
        /// </summary>
        /// <param name="events"></param>
        public MonitorController(IGroupRegistration events) {
            _events = events;
        }

        /// <summary>
        /// Subscribe to receive samples
        /// </summary>
        /// <remarks>
        /// Register a client to receive publisher samples through SignalR.
        /// </remarks>
        /// <param name="endpointId">The endpoint to subscribe to</param>
        /// <param name="userId">The user id that will receive publisher
        /// samples.</param>
        /// <returns></returns>
        [HttpPut("{endpointId}/samples")]
        public async Task SubscribeAsync(string endpointId,
            [FromBody] string userId) {
            await _events.SubscribeAsync(endpointId, userId);
        }

        /// <summary>
        /// Unsubscribe from receiving samples.
        /// </summary>
        /// <remarks>
        /// Unregister a client and stop it from receiving samples.
        /// </remarks>
        /// <param name="endpointId">The endpoint to unsubscribe from
        /// </param>
        /// <param name="userId">The user id that will not receive
        /// any more published samples</param>
        /// <returns></returns>
        [HttpDelete("{endpointId}/samples/{userId}")]
        public async Task UnsubscribeAsync(string endpointId, string userId) {
            await _events.UnsubscribeAsync(endpointId, userId);
        }

        private readonly IGroupRegistration _events;
    }
}
