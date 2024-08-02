// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Stack
{
    using System;

    /// <summary>
    /// The session handle
    /// </summary>
    public interface ISessionHandle : IDisposable
    {
        /// <summary>
        /// Session
        /// </summary>
        public IOpcUaSession Session { get; }

        /// <summary>
        /// Service call timeout
        /// </summary>
        TimeSpan ServiceCallTimeout { get; }
    }
}
