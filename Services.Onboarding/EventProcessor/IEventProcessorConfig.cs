// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcTwin.Services.Onboarding.EventHub {
    using System;

    /// <summary>
    /// Eventprocessor configuration
    /// </summary>
    public interface IEventProcessorConfig {

        /// <summary>
        /// Receive batch size
        /// </summary>
        int ReceiveBatchSize { get; }

        /// <summary>
        /// Receive timeout
        /// </summary>
        TimeSpan ReceiveTimeout { get; }

        /// <summary>
        /// Event hub connection string
        /// </summary>
        string EventHubConnString { get; }

        /// <summary>
        /// Event hub path
        /// </summary>
        string EventHubPath { get; }

        /// <summary>
        /// Consumer group
        /// </summary>
        string ConsumerGroup { get; }

        /// <summary>
        /// Blob storage connection string for checkpointing
        /// </summary>
        string BlobStorageConnString { get; }

        /// <summary>
        /// And lease container name. If null, use other means.
        /// </summary>
        string LeaseContainerName { get; }

        /// <summary>
        /// Whether to use websockets
        /// </summary>
        bool UseWebsockets { get; }
    }
}