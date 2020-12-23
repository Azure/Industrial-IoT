﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Microsoft.Azure.IIoT.OpcUa.Protocol.Models;
    using System;
    using System.Collections.Generic;
    using Opc.Ua;

    /// <summary>
    /// Data set message emitted by writer in a writer group.
    /// </summary>
    public class DataSetMessageModel {

        /// <summary>
        /// Sequence number of the event
        /// </summary>
        public uint SequenceNumber { get; set; }

        /// <summary>
        /// Publisher id
        /// </summary>
        public string PublisherId { get; set; }

        /// <summary>
        /// Dataset writer model reference
        /// </summary>
        public DataSetWriterModel Writer { get; set; }

        /// <summary>
        /// Writer group model reference
        /// </summary>
        public WriterGroupModel WriterGroup { get; set; }

        /// <summary>
        /// Timestamp of when this message was created
        /// </summary>
        public DateTime? TimeStamp { get; set; }

        /// <summary>
        /// Service message context
        /// </summary>
        public ServiceMessageContext ServiceMessageContext { get; set; }

        /// <summary>
        /// Monitored Item Notification received from the subscription.
        /// </summary>
        public IEnumerable<MonitoredItemNotificationModel> Notifications { get; set; }

        /// <summary>
        /// Subscription from which message originated
        /// </summary>
        public string SubscriptionId { get; set; }

        /// <summary>
        /// Endpoint the subscription is connected to
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Appplication url of the server the subscription is connected to.
        /// </summary>
        public string ApplicationUri { get; set; }
    }
}