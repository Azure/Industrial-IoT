// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;

    /// <summary>
    /// Collection for heartbeats from different workers
    /// </summary>
    public interface IJobHeartbeatCollection {
        /// <summary>
        /// Gets the items in the collection.
        /// </summary>
        IReadOnlyDictionary<string, JobHeartbeatModel> Heartbeats { get; }

        /// <summary>
        /// Adds an item to the collection or udpates it if it's already there.
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="heartbeat"></param>
        /// <returns></returns>
        public Task AddOrUpdate(string jobId, JobHeartbeatModel heartbeat);

        /// <summary>
        /// Removes the job heartbeats for given id if available.
        /// </summary>
        /// <param name="jobId"></param>
        /// <returns></returns>
        Task Remove(string jobId);
    }
}