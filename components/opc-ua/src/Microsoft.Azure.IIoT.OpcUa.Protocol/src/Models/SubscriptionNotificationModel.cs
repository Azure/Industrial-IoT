// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using System.Collections.Generic;
    using Opc.Ua;
    using System;
    using Opc.Ua.PubSub;

    /// <summary>
    /// Subscription notification model
    /// </summary>
    public class SubscriptionNotificationModel {

        /// <summary>
        /// Service message context
        /// </summary>
        public IServiceMessageContext ServiceMessageContext { get; set; }

        /// <summary>
        /// Notification
        /// </summary>
        public List<MonitoredItemNotificationModel> Notifications { get; set; }

        /// <summary>
        /// Message type
        /// </summary>
        public MessageType MessageType { get; set; }

        /// <summary>
        /// Meta data
        /// </summary>
        public DataSetMetaDataType MetaData { get; set; }

        /// <summary>
        /// Subscription from which message originated
        /// </summary>
        public string SubscriptionName { get; set; }

        /// <summary>
        /// Subscription identifier
        /// </summary>
        public ushort SubscriptionId { get; set; }

        /// <summary>
        /// Endpoint url
        /// </summary>
        public string EndpointUrl { get; set; }

        /// <summary>
        /// Appplication url
        /// </summary>
        public string ApplicationUri { get; set; }

        /// <summary>
        /// Publishing time
        /// </summary>
        public DateTime Timestamp { get; set; }
    }
}