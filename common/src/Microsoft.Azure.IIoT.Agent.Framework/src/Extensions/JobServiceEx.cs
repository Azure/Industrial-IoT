// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Job service extensions
    /// </summary>
    public static class JobServiceEx {

        /// <summary>
        /// List all jobs
        /// </summary>
        /// <param name="service"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<JobInfoModel>> ListAllJobsAsync(
            this IJobService service, CancellationToken ct = default) {
            string continuationToken = null;
            var jobs = new List<JobInfoModel>();
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
        /// Query and return all jobs
        /// </summary>
        /// <param name="service"></param>
        /// <param name="query"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<JobInfoModel>> QueryAllJobsAsync(
            this IJobService service, JobInfoQueryModel query, CancellationToken ct = default) {
            var jobs = new List<JobInfoModel>();
            var result = await service.QueryJobsAsync(query, null, ct);
            if (result.Jobs != null) {
                jobs.AddRange(result.Jobs);
            }
            var continuationToken = result.ContinuationToken;
            while (continuationToken != null) {
                result = await service.ListJobsAsync(continuationToken, null, ct);
                if (result.Jobs != null) {
                    jobs.AddRange(result.Jobs);
                }
                continuationToken = result.ContinuationToken;
            }
            return jobs;
        }
    }
}