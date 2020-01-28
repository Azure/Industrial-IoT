// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Monitored item sample message
    /// </summary>
    public class MonitoredItemSampleModel {

        /// <summary>
        /// Node id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public JToken Value { get; set; }

        /// <summary>
        /// Value's Status code string representation 
        /// </summary>
        public string Status { get; set; }
        
        /// <summary>
        /// Type id
        /// </summary>
        public string TypeId { get; set; }

        /// <summary>
        /// Source time stamp
        /// </summary>
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Source pico
        /// </summary>
        public ushort? SourcePicoseconds { get; set; }

        /// <summary>
        /// Server time stamp
        /// </summary>
        public DateTime? ServerTimestamp { get; set; }

        /// <summary>
        /// Server pico
        /// </summary>
        public ushort? ServerPicoseconds { get; set; }

        /// <summary>
        /// Sent time stamp
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Subscription or Dataset writer id
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Endpoint identifier
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Dataset id
        /// </summary>
        public string DataSetId { get; set; }
    }
}