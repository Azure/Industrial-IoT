// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Filters;
    using Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.AspNetCore.Mvc;
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Agent heartbeat controller
    /// </summary>
    [Route(VersionInfo.PATH + "/heartbeat")]
    [ExceptionsFilter]
    [Produces(ContentMimeType.Json)]
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
        /// <returns>Result of posting heartbeat information</returns>
        [HttpPost]
        public async Task<HeartbeatResponseApiModel> SendHeartbeatAsync(
            [FromBody] HeartbeatApiModel heartbeat) {
            if (heartbeat == null) {
                throw new ArgumentNullException(nameof(heartbeat));
            }
            var result = await _orchestrator.SendHeartbeatAsync(heartbeat.ToServiceModel());
            return new HeartbeatResponseApiModel(result);
        }

        private readonly IJobOrchestrator _orchestrator;
    }
}