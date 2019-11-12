// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Auth;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Filters;
    using Microsoft.Azure.IIoT.Services.OpcUa.Registry.v2.Models;
    using Microsoft.Azure.IIoT.OpcUa.Registry;
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Microsoft.Azure.IIoT.Messaging;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Configure persistent discovery
    /// </summary>
    [Route(VersionInfo.PATH + "/discovery")]
    [ExceptionsFilter]
    [Produces(ContentMimeType.Json)]
    [Authorize(Policy = Policies.CanQuery)]
    [ApiController]
    public class DiscoveryController : ControllerBase {

        /// <summary>
        /// Create controller for discovery services
        /// </summary>
        /// <param name="supervisors"></param>
        /// <param name="events"></param>
        public DiscoveryController(ISupervisorRegistry supervisors,
            IGroupRegistration events) {
            _supervisors = supervisors;
            _events = events;
        }

        /// <summary>
        /// Subscribe to discovery progress from supervisor
        /// </summary>
        /// <remarks>
        /// Register a client to receive discovery progress events
        /// through SignalR from a particular supervisor.
        /// </remarks>
        /// <param name="supervisorId">The supervisor to subscribe to</param>
        /// <param name="userId">The user id that will receive discovery
        /// events.</param>
        /// <returns></returns>
        [HttpPut("{supervisorId}/events")]
        public async Task SubscribeBySupervisorIdAsync(string supervisorId,
            [FromBody] string userId) {
            await _events.SubscribeAsync(supervisorId, userId);
        }

        /// <summary>
        /// Subscribe to discovery progress for a request
        /// </summary>
        /// <remarks>
        /// Register a client to receive discovery progress events
        /// through SignalR for a particular request.
        /// </remarks>
        /// <param name="requestId">The request to monitor</param>
        /// <param name="userId">The user id that will receive discovery
        /// events.</param>
        /// <returns></returns>
        [HttpPut("requests/{requestId}/events")]
        public async Task SubscribeByRequestIdAsync(string requestId,
            [FromBody] string userId) {
            await _events.SubscribeAsync(requestId, userId);
        }

        /// <summary>
        /// Enable server discovery
        /// </summary>
        /// <remarks>
        /// Allows a caller to configure recurring discovery runs on the
        /// discovery module identified by the module id.
        /// </remarks>
        /// <param name="supervisorId">supervisor identifier</param>
        /// <param name="mode">Discovery mode</param>
        /// <param name="config">Discovery configuration</param>
        [HttpPost("{supervisorId}")]
        [Authorize(Policy = Policies.CanChange)]
        public async Task SetDiscoveryModeAsync(string supervisorId,
            [FromQuery] [Required] DiscoveryMode mode,
            [FromBody] DiscoveryConfigApiModel config) {
            await _supervisors.UpdateSupervisorAsync(supervisorId,
                new SupervisorUpdateModel {
                    Discovery = mode,
                    DiscoveryConfig = config?.ToServiceModel()
                });
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
        /// <param name="userId">The user id that will not receive
        /// any more discovery progress</param>
        /// <returns></returns>
        [HttpDelete("requests/{requestId}/events/{userId}")]
        public async Task UnsubscribeByRequestIdAsync(string requestId,
            string userId) {
            await _events.UnsubscribeAsync(requestId, userId);
        }

        /// <summary>
        /// Unsubscribe from discovery progress from supervisor.
        /// </summary>
        /// <remarks>
        /// Unregister a client and stop it from receiving discovery events.
        /// </remarks>
        /// <param name="supervisorId">The supervisor to unsubscribe from
        /// </param>
        /// <param name="userId">The user id that will not receive
        /// any more discovery progress</param>
        /// <returns></returns>
        [HttpDelete("{supervisorId}/events/{userId}")]
        public async Task UnsubscribeBySupervisorIdAsync(string supervisorId,
            string userId) {
            await _events.UnsubscribeAsync(supervisorId, userId);
        }

        private readonly ISupervisorRegistry _supervisors;
        private readonly IGroupRegistration _events;
    }
}
