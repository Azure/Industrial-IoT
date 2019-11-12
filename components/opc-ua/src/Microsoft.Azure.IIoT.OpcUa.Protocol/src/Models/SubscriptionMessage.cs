// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Protocol.Models {
    using System.Collections.Generic;
    using Opc.Ua;

    /// <summary>
    /// Subscription message
    /// </summary>
    public class SubscriptionMessage {

        /// <summary>
        /// Service message context
        /// </summary>
        public ServiceMessageContext ServiceMessageContext { get; set; }

        /// <summary>
        /// Values
        /// </summary>
        public Dictionary<string, DataValue> Values { get; set; }
    }
}