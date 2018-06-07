// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {

    /// <summary>
    /// refer to Microsoft.Azure.Devices.DeviceJobStatus
    /// </summary>
    public enum DeviceJobStatus {

        Pending = 0,
        Scheduled = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Canceled = 5
    }
}
