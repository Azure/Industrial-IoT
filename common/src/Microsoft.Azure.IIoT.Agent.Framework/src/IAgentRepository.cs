// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Worker repository
    /// </summary>
    public interface IAgentRepository {

        /// <summary>
        /// Add or update heartbeat
        /// </summary>
        /// <param name="supervisorHeartbeat"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task AddOrUpdate(SupervisorHeartbeatModel supervisorHeartbeat, CancellationToken ct = default);
    }
}