// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {

    /// <summary>
    /// refer to Microsoft.Azure.Devices.JobStatus
    /// </summary>
    public enum JobStatus {
        Unknown = 0,
        Enqueued = 1,
        Running = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5,
        Scheduled = 6,
        Queued = 7
    }
}
