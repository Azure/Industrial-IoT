// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    using System;

    /// <summary>
    /// Flags indicating the publish state.
    /// </summary>
    [Flags]
    public enum PublishState
    {
        /// <summary>
        /// The publish state has not changed.
        /// </summary>
        None = 0,

        /// <summary>
        /// The publishing stopped.
        /// </summary>
        Stopped = 1 << 0,

        /// <summary>
        /// The publishing recovered.
        /// </summary>
        Recovered = 1 << 1,

        /// <summary>
        /// A keep alive message was received.
        /// </summary>
        KeepAlive = 1 << 2,

        /// <summary>
        /// A republish for a missing message was issued.
        /// </summary>
        Republish = 1 << 3,

        /// <summary>
        /// The publishing was transferred to another node.
        /// </summary>
        Transferred = 1 << 4,

        /// <summary>
        /// The publishing was timed out
        /// </summary>
        Timeout = 1 << 5,
    }
}
