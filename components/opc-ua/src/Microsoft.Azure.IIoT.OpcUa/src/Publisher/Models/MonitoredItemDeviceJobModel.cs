// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {

    /// <summary>
    /// Job to publish messages as a device to iothub
    /// </summary>
    public class MonitoredItemDeviceJobModel {

        /// <summary>
        /// Job description
        /// </summary>
        public MonitoredItemJobModel Job { get; set; }

        /// <summary>
        /// Connection string for the job
        /// </summary>
        public string ConnectionString { get; set; }
    }
}