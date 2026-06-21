// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Services
{
    /// <summary>
    /// Process-global lock used by the test server infrastructure to serialize
    /// access to non-thread-safe, process-global OPC UA stack state.
    /// <para>
    /// Starting a server builds its address space and registers predefined
    /// nodes/types into process-global stack state, and a running server's
    /// condition-refresh worker mutates branch/condition state that resolves
    /// against that same global state. When multiple test servers run
    /// concurrently (different fixtures in parallel test collections), one
    /// server's startup can race another server's condition refresh and produce
    /// a torn read that crashes the test host with an access violation in
    /// <c>Opc.Ua.ConditionState.IsBranch()</c>. Acquiring this single lock on
    /// both sides makes server startup and branch-managing condition refresh
    /// mutually exclusive across all server instances in the process.
    /// </para>
    /// </summary>
    public static class ServerStateLock
    {
        /// <summary>
        /// Synchronization object. Hold it around server startup and around
        /// branch-managing <c>ConditionRefresh</c>. Always acquire this lock
        /// before any per-node-manager lock to keep a consistent lock order.
        /// </summary>
        public static readonly object Sync = new();
    }
}
