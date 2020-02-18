// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Agent manager extensions
    /// </summary>
    public static class AgentManagerEx {

        /// <summary>
        /// List all agents
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        public static async Task<IEnumerable<WorkerSupervisorInfoModel>> ListAllAgentsAsync(
            this IWorkerSupervisorRegistry manager, CancellationToken ct = default) {
            string continuationToken = null;
            var agents = new List<WorkerSupervisorInfoModel>();
            do {
                var result = await manager.ListWorkerSupervisorsAsync(continuationToken, null, ct);
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