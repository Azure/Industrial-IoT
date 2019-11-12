// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Filters;
    using Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.v2.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Workers jobs controller
    /// </summary>
    [Route(VersionInfo.PATH + "/workers")]
    [ExceptionsFilter]
    [Produces(ContentMimeType.Json)]
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
        /// <returns></returns>
        [HttpPost("{workerId}")]
        public async Task<JobProcessingInstructionApiModel> GetAvailableJobAsync(
            string workerId, [FromBody] JobRequestApiModel request) {
            if (string.IsNullOrEmpty(workerId)) {
                throw new ArgumentNullException(nameof(workerId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var job = await _orchestrator.GetAvailableJobAsync(
                workerId, request.ToServiceModel());
            return new JobProcessingInstructionApiModel(job);
        }

        private readonly IJobOrchestrator _orchestrator;
    }
}