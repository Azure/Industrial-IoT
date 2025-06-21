// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Specifies the transport technology used to publish messages from OPC Publisher.
    /// Each transport offers different capabilities for message delivery, routing,
    /// and quality of service. The transport choice affects how messages are delivered
    /// and what features are available.
    /// </summary>
    [DataContract]
    public enum WriterGroupTransport
    {
        /// <summary>
        /// Default transport using Azure IoT Hub or IoT Edge.
        /// Provides secure, reliable message delivery to Azure cloud.
        /// Supports message batching, compression, and QoS.
        /// Message size limited to 256KB by IoT Hub.
        /// </summary>
        [EnumMember(Value = "IoTHub")]
        IoTHub,

        /// <summary>
        /// Publish to any MQTT broker using MQTT v3.1.1 protocol.
        /// Supports configurable topics and QoS levels.
        /// Compatible with most MQTT brokers (Mosquitto, HiveMQ, etc.).
        /// Good choice for local network publishing.
        /// </summary>
        [EnumMember(Value = "Mqtt")]
        Mqtt,

        /// <summary>
        /// Direct publishing to Azure Event Hubs.
        /// Designed for high-throughput data ingestion.
        /// Supports partitioning for parallel processing.
        /// Suitable for big data and analytics scenarios.
        /// </summary>
        [EnumMember(Value = "EventHub")]
        EventHub,

        /// <summary>
        /// Publishing through Dapr pub/sub building block.
        /// Provides transport abstraction and flexibility.
        /// Supports multiple message brokers through components.
        /// Ideal for cloud-native and microservices architectures.
        /// </summary>
        [EnumMember(Value = "Dapr")]
        Dapr,

        /// <summary>
        /// Publish messages to HTTP endpoints (webhooks).
        /// Supports configurable endpoints and authentication.
        /// Messages sent as HTTP POST requests.
        /// Useful for integration with web services and APIs.
        /// </summary>
        [EnumMember(Value = "Http")]
        Http,

        /// <summary>
        /// Write messages to local or network file system.
        /// Supports various file formats and patterns.
        /// Useful for logging, debugging, or offline scenarios.
        /// Can integrate with file-based processing systems.
        /// </summary>
        [EnumMember(Value = "FileSystem")]
        FileSystem,

        /// <summary>
        /// Messages are discarded without being published.
        /// Used for testing or when only monitoring is needed.
        /// No external dependencies or configuration required.
        /// Minimal resource usage as messages are dropped.
        /// </summary>
        [EnumMember(Value = "Null")]
        Null
    }
}
