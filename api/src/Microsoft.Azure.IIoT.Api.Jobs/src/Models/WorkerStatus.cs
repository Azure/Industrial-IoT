// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Worker state
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum WorkerStatus {

        /// <summary>
        /// Stopped
        /// </summary>
        Stopped,

        /// <summary>
        /// Stopping
        /// </summary>
        Stopping,

        /// <summary>
        /// Waiting
        /// </summary>
        WaitingForJob,

        /// <summary>
        /// Processing
        /// </summary>
        ProcessingJob
    }
}