// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    /// <summary>
    /// Heartbeat
    /// </summary>
    public enum HeartbeatInstruction {

        /// <summary>
        /// Keep
        /// </summary>
        Keep,

        /// <summary>
        /// Switch to active
        /// </summary>
        SwitchToActive,

        /// <summary>
        /// Cancel processing
        /// </summary>
        CancelProcessing
    }
}