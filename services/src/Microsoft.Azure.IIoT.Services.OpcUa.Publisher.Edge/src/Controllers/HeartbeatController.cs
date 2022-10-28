// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Edge.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Edge.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;
    using System.Threading;

    /// <summary>
    /// Agent heartbeat controller
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/heartbeat")]
    [ExceptionsFilter]
    [ApiController]
    public class HeartbeatController : ControllerBase {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="orchestrator"></param>
        public HeartbeatController(IJobOrchestrator orchestrator) {
            _orchestrator = orchestrator;
        }

        /// <summary>
        /// Post heartbeat information
        /// </summary>
        /// <remarks>
        /// Allows the agent to post heartbeat information
        /// </remarks>
        /// <param name="heartbeat">The heartbeat information</param>
        /// <param name="ct">cancellation token</param>
        /// <returns>Result of posting heartbeat information</returns>
        [HttpPost]
        public async Task<HeartbeatResponseApiModel> SendHeartbeatAsync(
            [FromBody] HeartbeatApiModel heartbeat, CancellationToken ct) {
            if (heartbeat == null) {
                throw new ArgumentNullException(nameof(heartbeat));
            }
            var result = await _orchestrator.SendHeartbeatAsync(
                heartbeat.ToServiceModel(), ct:ct);
            return result.ToApiModel();
        }

        private readonly IJobOrchestrator _orchestrator;
    }
}