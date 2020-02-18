// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Worker state
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SupervisorStatus {

        /// <summary>
        /// Stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// Stopping
        /// </summary>
        Stopping,

        /// <summary>
        /// Running
        /// </summary>
        Running
    }
}