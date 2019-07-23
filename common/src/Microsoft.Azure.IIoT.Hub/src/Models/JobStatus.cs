// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// refer to Microsoft.Azure.Devices.JobStatus
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum JobStatus {

        /// <summary>
        /// Unknown job status
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Job enqueued
        /// </summary>
        Enqueued = 1,

        /// <summary>
        /// Job is running
        /// </summary>
        Running = 2,

        /// <summary>
        /// Job was completed
        /// </summary>
        Completed = 3,

        /// <summary>
        /// Job has failed
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Job was cancelled
        /// </summary>
        Cancelled = 5,

        /// <summary>
        /// Job scheduled
        /// </summary>
        Scheduled = 6,

        /// <summary>
        /// Job queued
        /// </summary>
        Queued = 7
    }
}
