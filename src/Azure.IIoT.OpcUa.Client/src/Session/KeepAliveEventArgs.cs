// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// The delegate used to receive keep alive notifications.
    /// </summary>
    /// <param name="session"></param>
    /// <param name="e"></param>
    public delegate void KeepAliveEventHandler(ISession session,
        KeepAliveEventArgs e);

    /// <summary>
    /// The event arguments provided when a keep alive response arrives.
    /// </summary>
    public sealed class KeepAliveEventArgs : EventArgs
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        /// <param name="status"></param>
        /// <param name="currentState"></param>
        /// <param name="currentTime"></param>
        public KeepAliveEventArgs(ServiceResult status, ServerState currentState,
            DateTime currentTime)
        {
            Status = status;
            CurrentState = currentState;
            CurrentTime = currentTime;
        }

        /// <summary>
        /// Gets the status associated with the keep alive operation.
        /// </summary>
        public ServiceResult Status { get; }

        /// <summary>
        /// Gets the current server state.
        /// </summary>
        public ServerState CurrentState { get; }

        /// <summary>
        /// Gets the current server time.
        /// </summary>
        public DateTime CurrentTime { get; }

        /// <summary>
        /// Gets or sets a flag indicating whether the session should send another keep alive.
        /// </summary>
        public bool CancelKeepAlive { get; set; }
    }
}
