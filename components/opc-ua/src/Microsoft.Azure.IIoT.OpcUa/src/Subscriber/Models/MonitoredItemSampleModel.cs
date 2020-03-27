// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {
    using System;


    /// <summary>
    /// Monitored item sample message
    /// </summary>
    public class MonitoredItemSampleModel {

        /// <summary>
        /// Publisher Id
        /// </summary>
        public string PublisherId { get; set; }


        /// <summary>
        /// Dataset writer id
        /// </summary>
        public string DataSetWriterId { get; set; }

        /// <summary>
        /// Endpoint identifier
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public dynamic Value { get; set; }

        /// <summary>
        /// Type id
        /// </summary>
        public Type TypeId { get; set; }

        /// <summary>
        /// Value's Status code string representation 
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Publisher's time stamp
        /// </summary>
        public DateTime? Timestamp { get; set; }

        /// <summary>
        /// Source time stamp
        /// </summary>
        public DateTime? SourceTimestamp { get; set; }

        /// <summary>
        /// Server time stamp
        /// </summary>
        public DateTime? ServerTimestamp { get; set; }
    }
}