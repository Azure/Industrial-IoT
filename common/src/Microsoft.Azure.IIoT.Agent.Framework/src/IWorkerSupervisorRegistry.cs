// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Worker supervisorRegistry
    /// </summary>
    public interface IWorkerSupervisorRegistry {

        /// <summary>
        /// List workers
        /// </summary>
        /// <param name="continuationToken"></param>
        /// <param name="maxPageSize"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WorkerSupervisorInfoListModel> ListWorkerSupervisorsAsync(
            string continuationToken = null, int? maxPageSize = null,
            CancellationToken ct = default);

        /// <summary>
        /// Get worker by id
        /// </summary>
        /// <param name="workerSupervisorId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<WorkerSupervisorInfoModel> GetWorkerSupervisorAsync(string workerSupervisorId,
            CancellationToken ct = default);

        /// <summary>
        /// Delete worker
        /// </summary>
        /// <param name="workerSupervisorId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task DeleteWorkerSupervisorAsync(string workerSupervisorId,
            CancellationToken ct = default);
    }
}