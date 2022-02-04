// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher {
    using Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job service api extensions
    /// </summary>
    public static class JobsServiceApiEx {

        /// <summary>
        /// List all jobs
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<JobInfoApiModel>> ListAllJobsAsync(
            this IPublisherJobServiceApi service, CancellationToken ct = default) {
            string continuationToken = null;
            var jobs = new List<JobInfoApiModel>();
            do {
                var result = await service.ListJobsAsync(continuationToken, null, ct);
                if (result.Jobs != null) {
                    jobs.AddRange(result.Jobs);
                }
                continuationToken = result.ContinuationToken;
            }
            while (continuationToken != null);
            return jobs;
        }

        /// <summary>
        /// List all jobs
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<JobInfoApiModel>> QueryAllJobsAsync(
            this IPublisherJobServiceApi service, JobInfoQueryApiModel query, CancellationToken ct = default) {
            var jobs = new List<JobInfoApiModel>();
            var result = await service.QueryJobsAsync(query, null, ct);
            if (result.Jobs != null) {
                jobs.AddRange(result.Jobs);
            }
            while (result.ContinuationToken != null) {
                result = await service.ListJobsAsync(result.ContinuationToken, null, ct);
                if (result.Jobs != null) {
                    jobs.AddRange(result.Jobs);
                }
            }
            return jobs;
        }

        /// <summary>
        /// List all agents
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<WorkerInfoApiModel>> ListAllAgentsAsync(
            this IPublisherJobServiceApi service, CancellationToken ct = default) {
            string continuationToken = null;
            var agents = new List<WorkerInfoApiModel>();
            do {
                var result = await service.ListWorkersAsync(continuationToken, null, ct);
                if (result.Workers != null) {
                    agents.AddRange(result.Workers);
                }
                continuationToken = result.ContinuationToken;
            }
            while (continuationToken != null);
            return agents;
        }
    }

}