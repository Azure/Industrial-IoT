// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System.Collections.Generic;

    /// <summary>
    /// Monitored item message job
    /// </summary>
    public class MonitoredItemMessageContentModel {

        /// <summary>
        /// Fields that should be encoded in the message
        /// </summary>
        public MonitoredItemMessageContentMask? Fields { get; set; }

        /// <summary>
        /// Content encoding for published messages
        /// </summary>
        public MonitoredItemMessageEncoding? Encoding { get; set; }

        /// <summary>
        /// Properties to include in the message
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }
    }
}