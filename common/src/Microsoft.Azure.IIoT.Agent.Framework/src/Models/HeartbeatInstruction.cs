// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Heartbeat
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
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