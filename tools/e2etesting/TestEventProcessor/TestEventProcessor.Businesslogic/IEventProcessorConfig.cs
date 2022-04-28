// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace TestEventProcessor.Businesslogic {

    /// <summary>
    /// Configuration for EventProcessorWrapper.
    /// </summary>
    public interface IEventProcessorConfig {
        /// <summary>
        /// Gets or sets the connection string of the EventHub-Endpoint of the IoT Hub.
        /// </summary>
        string IoTHubEventHubEndpointConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the connection string of the storage account required to enable checkpointing (even if mode is set to 'Latest')
        /// </summary>
        string StorageConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the name of the blob container in the storage account.
        /// </summary>
        string BlobContainerName { get; set; }

        /// <summary>
        /// Gets ot sets the name of the Event Hub Consumer group.
        /// </summary>
        string EventHubConsumerGroup { get; set; }
    }
}
