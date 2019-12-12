﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Subscriber.Models {
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Publisher telemetry
    /// </summary>
    public class SubscriberSampleModel {

        /// <summary>
        /// Subscription id
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Endpoint
        /// </summary>
        public string EndpointId { get; set; }

        /// <summary>
        /// Dataset id
        /// </summary>
        public string DataSetId { get; set; }

        /// <summary>
        /// Node id
        /// </summary>
        public string NodeId { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        public JToken Value { get; set; }

        /// <summary>
        /// Sent time stamp
        /// </summary>
        public DateTime? Timestamp { get; set; }

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
    }
}