// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Connector to job manager
    /// </summary>
    public interface IJobOrchestrator {

        /// <summary>
        /// Get available job
        /// </summary>
        /// <param name="workerId"></param>
        /// <param name="request"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<JobProcessingInstructionModel> GetAvailableJobAsync(string workerId,
            JobRequestModel request, CancellationToken ct = default);

        /// <summary>
        /// Send heartbeat
        /// </summary>
        /// <param name="heartbeat"></param>
        /// <param name="diagInfo"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<HeartbeatResultModel> SendHeartbeatAsync(HeartbeatModel heartbeat,
            JobDiagnosticInfoModel diagInfo = null,
            CancellationToken ct = default);
    }
}