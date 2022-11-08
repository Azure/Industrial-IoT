// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Edge.Controllers {
    using Microsoft.Azure.IIoT.Services.OpcUa.Publisher.Edge.Filters;
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using System;
    using System.Threading;

    /// <summary>
    /// Workers jobs controller
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/workers")]
    [ExceptionsFilter]
    [ApiController]
    public class WorkersController : ControllerBase {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="orchestrator"></param>
        public WorkersController(IJobOrchestrator orchestrator) {
            _orchestrator = orchestrator;
        }

        /// <summary>
        /// Get processing instructions for worker
        /// </summary>
        /// <param name="workerId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        [HttpPost("{workerId}")]
        public async Task<JobProcessingInstructionApiModel> GetAvailableJobAsync(
            string workerId, [FromBody] JobRequestApiModel request, CancellationToken ct) {
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException(nameof(workerId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var job = await _orchestrator.GetAvailableJobAsync(
                workerId, request.ToServiceModel(), ct);
            return job.ToApiModel();
        }

        private readonly IJobOrchestrator _orchestrator;
    }
}