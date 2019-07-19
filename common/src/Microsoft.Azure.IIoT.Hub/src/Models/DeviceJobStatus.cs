// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// refer to Microsoft.Azure.Devices.DeviceJobStatus
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DeviceJobStatus {

        /// <summary>
        /// Device job is pending
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Device job is scheduled
        /// </summary>
        Scheduled = 1,

        /// <summary>
        /// Device job is running
        /// </summary>
        Running = 2,

        /// <summary>
        /// Device job is completed
        /// </summary>
        Completed = 3,

        /// <summary>
        /// Device job has failed.
        /// </summary>
        Failed = 4,

        /// <summary>
        /// Device job was cancelled.
        /// </summary>
        Cancelled = 5
    }
}
