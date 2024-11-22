// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation, The OPC Foundation, Inc.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Opc.Ua.Client
{
    /// <summary>
    /// Session state
    /// </summary>
    public enum SessionState
    {
        /// <summary>
        /// Session is connecting
        /// </summary>
        Connecting,

        /// <summary>
        /// Connect failed
        /// </summary>
        FailedRetrying,

        /// <summary>
        /// Session is connected
        /// </summary>
        Connected,

        /// <summary>
        /// Session is disconnected
        /// </summary>
        Disconnected,

        /// <summary>
        /// Connect hit fatal error
        /// </summary>
        ConnectError,

        /// <summary>
        /// Session is closed
        /// </summary>
        Closed
    }
}
