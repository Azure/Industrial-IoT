// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.Common.Jobs.v2.Models;
    using Microsoft.Azure.IIoT.Services.Common.Jobs.v2.Filters;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Swagger;
    using System.Threading.Tasks;
    using System.Linq;
    using System;

    /// <summary>
    /// Jobs controller
    /// </summary>
    [Route(VersionInfo.PATH + "/jobs")]
    [ExceptionsFilter]
    [Produces(ContentMimeType.Json)]
    [ApiController]
    public class JobsController : ControllerBase {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="jobManager"></param>
        public JobsController(IJobService jobManager) {
            _jobManager = jobManager;
        }

        /// <summary>
        /// Get list of jobs
        /// </summary>
        /// <remarks>
        /// List all jobs that are registered or continues a query.
        /// </remarks>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Jobs</returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<JobInfoListApiModel> ListJobsAsync(
            [FromQuery] string continuationToken,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.ContinuationToken)) {
                continuationToken = Request.Headers[HttpHeader.ContinuationToken]
                    .FirstOrDefault();
            }
            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _jobManager.ListJobsAsync(
                continuationToken, pageSize);
            return new JobInfoListApiModel(result);
        }

        /// <summary>
        /// Query jobs
        /// </summary>
        /// <remarks>
        /// List all jobs that are registered or continues a query.
        /// </remarks>
        /// <param name="query">Query specification to use as filter.</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Jobs</returns>
        [HttpPost]
        public async Task<JobInfoListApiModel> QueryJobsAsync(
            [FromBody] JobInfoQueryApiModel query,
            [FromQuery] int? pageSize) {

            if (Request.Headers.ContainsKey(HttpHeader.MaxItemCount)) {
                pageSize = int.Parse(Request.Headers[HttpHeader.MaxItemCount]
                    .FirstOrDefault());
            }
            var result = await _jobManager.QueryJobsAsync(
                query.ToServiceModel(), pageSize);
            return new JobInfoListApiModel(result);
        }

        /// <summary>
        /// Get job by id
        /// </summary>
        /// <remarks>
        /// Returns a job with the provided identifier.
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<JobInfoApiModel> GetJobAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            var job = await _jobManager.GetJobAsync(id);
            return new JobInfoApiModel(job);
        }

        /// <summary>
        /// Cancel job by id
        /// </summary>
        /// <remarks>
        /// Cancels a job execution.
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}/cancel")]
        public async Task CancelJobAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            await _jobManager.CancelJobAsync(id);
        }

        /// <summary>
        /// Restart job by id
        /// </summary>
        /// <remarks>
        /// Restarts a cancelled job which sets it back to active.
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}/restart")]
        public async Task RestartJobAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            await _jobManager.RestartJobAsync(id);
        }

        /// <summary>
        /// Delete job by id
        /// </summary>
        /// <remarks>
        /// Deletes a job.
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task DeleteJobAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            await _jobManager.DeleteJobAsync(id);
        }

        private readonly IJobService _jobManager;
    }
}