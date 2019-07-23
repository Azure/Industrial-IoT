// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Desired module status
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModuleDesiredStatus {

        /// <summary>
        /// This is the state that all modules start out in.
        /// </summary>
        Unknown,

        /// <summary>
        /// Modules transition to the "Backoff" state when
        /// the agent has scheduled the module to be started
        /// but hasn't actually started running yet.
        /// </summary>
        Backoff,

        /// <summary>
        /// This state indicates that module is currently
        /// running.
        /// </summary>
        Running,

        /// <summary>
        /// The state transitions to "unhealthy" when a
        /// health-probe check fails/times out.
        /// </summary>
        Unhealthy,

        /// <summary>
        /// The Stopped state indicates that the module
        /// exited successfully (with a zero exit code).
        /// </summary>
        Stopped,

        /// <summary>
        /// The "Failed" state indicates that the module exited
        /// with a failure exit code (non-zero). The module can
        /// transition back to Backoff from this state depending
        /// on the restart policy in effect.
        /// </summary>
        Failed
    }
}
