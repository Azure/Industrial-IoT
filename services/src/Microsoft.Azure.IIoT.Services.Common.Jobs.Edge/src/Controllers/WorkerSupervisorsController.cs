// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.Controllers {
    using Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.Filters;
    using Microsoft.Azure.IIoT.Services.Common.Jobs.Edge.Models;
    using Microsoft.Azure.IIoT.Api.Jobs.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.AspNetCore.Mvc;
    using System.Threading.Tasks;
    using System;
    using System.Linq;

    /// <summary>
    /// Workers jobs controller
    /// </summary>
    [ApiVersion("2")][Route("v{version:apiVersion}/workersupervisors")]
    [ExceptionsFilter]
    [Produces(ContentMimeType.Json)]
    [ApiController]
    public class WorkerSupervisorsController : ControllerBase {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="orchestrator"></param>
        public WorkerSupervisorsController(IJobOrchestrator orchestrator) {
            _orchestrator = orchestrator;
        }

        /// <summary>
        /// Get processing instructions for worker
        /// </summary>
        /// <param name="workerSupervisorId"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost("{workerSupervisorId}")]
        public async Task<JobProcessingInstructionApiModel[]> GetAvailableJobAsync(
            string workerSupervisorId, [FromBody] JobRequestApiModel request) {
            if (string.IsNullOrEmpty(workerSupervisorId)) {
                throw new ArgumentNullException(nameof(workerSupervisorId));
            }
            if (request == null) {
                throw new ArgumentNullException(nameof(request));
            }
            var jobs = await _orchestrator.GetAvailableJobsAsync(
                workerSupervisorId, request.ToServiceModel());
            return jobs.Select(j => j.ToApiModel()).ToArray();
        }

        private readonly IJobOrchestrator _orchestrator;
    }
}