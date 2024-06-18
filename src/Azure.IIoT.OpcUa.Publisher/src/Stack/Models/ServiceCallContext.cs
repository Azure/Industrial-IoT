// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack.Models
{
    using System.Threading;

    /// <summary>
    /// Context for a service call invocation
    /// </summary>
    public sealed record class ServiceCallContext
    {
        /// <summary>
        /// The session
        /// </summary>
        public IOpcUaSession Session { get; }

        /// <summary>
        /// A continuation token to track after
        /// returning from the call.
        /// </summary>
        public string? TrackedToken { get; set; }

        /// <summary>
        /// A token to release from tracking after
        /// returning from the call.
        /// </summary>
        public string? UntrackedToken { get; set; }

        /// <summary>
        /// Cancel any calls on this token
        /// </summary>
        public CancellationToken Ct { get; }

        /// <summary>
        /// Create context
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ct"></param>
        internal ServiceCallContext(
            IOpcUaSession session, CancellationToken ct)
        {
            Session = session;
            Ct = ct;
        }
    }
}
