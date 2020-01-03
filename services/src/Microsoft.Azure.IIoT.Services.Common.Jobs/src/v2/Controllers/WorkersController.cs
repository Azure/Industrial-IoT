// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.Common.Jobs.v2.Controllers {
    using Microsoft.Azure.IIoT.Services.Common.Jobs.v2.Filters;
    using Microsoft.Azure.IIoT.Services.Common.Jobs.v2.Models;
    using Microsoft.Azure.IIoT.Agent.Framework;
    using Microsoft.Azure.IIoT.Http;
    using Microsoft.AspNetCore.Mvc;
    using Swashbuckle.AspNetCore.Swagger;
    using System.Linq;
    using System.Threading.Tasks;
    using System;

    /// <summary>
    /// Agent controller
    /// </summary>
    [Route(VersionInfo.PATH + "/workers")]
    [ExceptionsFilter]
    [Produces(ContentMimeType.Json)]
    [ApiController]
    public class WorkersController : ControllerBase {

        /// <summary>
        /// Create controller
        /// </summary>
        /// <param name="registry"></param>
        public WorkersController(IWorkerRegistry registry) {
            _registry = registry;
        }

        /// <summary>
        /// Get list of workers
        /// </summary>
        /// <remarks>
        /// List all workers that are registered or continues a query.
        /// </remarks>
        /// <param name="continuationToken">Optional Continuation
        /// token</param>
        /// <param name="pageSize">Optional number of results to
        /// return</param>
        /// <returns>Workers</returns>
        [HttpGet]
        [AutoRestExtension(NextPageLinkName = "continuationToken")]
        public async Task<WorkerInfoListApiModel> ListWorkersAsync(
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
            var result = await _registry.ListWorkersAsync(
                continuationToken, pageSize);
            return new WorkerInfoListApiModel(result);
        }

        /// <summary>
        /// Get worker
        /// </summary>
        /// <remarks>
        /// Returns a worker with the provided identifier.
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        public async Task<WorkerInfoApiModel> GetWorkerAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            var result = await _registry.GetWorkerAsync(id);
            return new WorkerInfoApiModel(result);
        }

        /// <summary>
        /// Delete worker by id
        /// </summary>
        /// <remarks>
        /// Deletes an worker in the registry.
        /// </remarks>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        public async Task DeleteWorkerAsync(string id) {
            if (string.IsNullOrEmpty(id)) {
                throw new ArgumentNullException(nameof(id));
            }
            await _registry.DeleteWorkerAsync(id);
        }

        private readonly IWorkerRegistry _registry;
    }
}