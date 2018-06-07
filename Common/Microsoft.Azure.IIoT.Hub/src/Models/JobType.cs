// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {

    /// <summary>
    /// refer to Microsoft.Azure.Devices.JobType
    /// </summary>
    public enum JobType {
        Unknown = 0,
        ScheduleDeviceMethod = 3,
        ScheduleUpdateTwin = 4
    }
}
