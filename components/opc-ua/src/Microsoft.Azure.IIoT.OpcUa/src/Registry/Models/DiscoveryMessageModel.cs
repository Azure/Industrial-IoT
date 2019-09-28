// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Discovery progress
    /// </summary>
    public class DiscoveryMessageModel {

        /// <summary>
        /// Timestamp of the discovery sweep.
        /// </summary>
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Index in the batch with same timestamp.
        /// </summary>
        public DiscoveryMessageType Event { get; set; }

        /// <summary>
        /// Message template
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Parameters of the event message
        /// </summary>
        public List<object> Parameters { get; set; }
    }
}
