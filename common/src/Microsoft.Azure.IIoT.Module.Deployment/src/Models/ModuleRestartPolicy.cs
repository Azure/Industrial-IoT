// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Deployment.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Module restart policy
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ModuleRestartPolicy {

        /// <summary>
        /// Never restart
        /// </summary>
        Never,

        /// <summary>
        /// Restart on failure
        /// </summary>
        OnFailure,

        /// <summary>
        /// Restart when unhealthy
        /// </summary>
        OnUnhealthy,

        /// <summary>
        /// Restart on graceful exit and failure
        /// </summary>
        Always
    }
}
