// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Messaging.EventHub {

    /// <summary>
    /// Event hub configuration
    /// </summary>
    public interface IEventHubConsumerConfig : IEventHubConfig {

        /// <summary>
        /// Whether to use websockets
        /// </summary>
        bool UseWebsockets { get; }

        /// <summary>
        /// Consumer group
        /// (optional, default to $default)
        /// </summary>
        string ConsumerGroup { get; }
    }
}
