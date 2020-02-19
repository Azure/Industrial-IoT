// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Agent {
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.IIoT.Agent.Framework.Models;

    /// <summary>
    /// An implementation of IJobHeartbeatCollection that uses an in-memory ConcurrentDictionary.
    /// </summary>
    public class InMemoryJobHeartbeatCollection : IJobHeartbeatCollection {
        /// <inheritdoc />
        public IReadOnlyDictionary<string, JobHeartbeatModel> Heartbeats => _heartbeats;

        /// <inheritdoc />
        public Task AddOrUpdate(string jobId, JobHeartbeatModel heartbeat) {
            _heartbeats.AddOrUpdate(jobId, heartbeat, (a, c) => c);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Remove(string jobId) {
            _heartbeats.TryRemove(jobId, out var value);
            return Task.CompletedTask;
        }

        private readonly ConcurrentDictionary<string, JobHeartbeatModel> _heartbeats = new ConcurrentDictionary<string, JobHeartbeatModel>();
    }
}