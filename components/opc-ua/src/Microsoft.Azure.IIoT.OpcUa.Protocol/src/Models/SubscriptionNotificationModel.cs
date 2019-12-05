﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using System.Collections.Generic;
    using Opc.Ua;

    /// <summary>
    /// Subscription notification model
    /// </summary>
    public class SubscriptionNotificationModel {

        /// <summary>
        /// Service message context
        /// </summary>
        public ServiceMessageContext ServiceMessageContext { get; set; }

        /// <summary>
        /// Notification
        /// </summary>
        public List<MonitoredItemNotificationModel> Notifications { get; set; }

        /// <summary>
        /// Subscription from which message originated
        /// </summary>
        public string SubscriptionId { get; internal set; }

        /// <summary>
        /// Endpoint url
        /// </summary>
        public string EndpointUrl { get; internal set; }

        /// <summary>
        /// Appplication url
        /// </summary>
        public string ApplicationUri { get; internal set; }
    }
}