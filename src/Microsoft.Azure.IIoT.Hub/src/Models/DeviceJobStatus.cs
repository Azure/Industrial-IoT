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

        Pending = 0,
        Scheduled = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }
}
