// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Defines how OPC UA Publisher formats and structures messages for transport.
    /// Each mode provides different trade-offs between message completeness,
    /// bandwidth efficiency, and compatibility with different message consumers.
    /// </summary>
    [DataContract]
    public enum MessagingMode
    {
        /// <summary>
        /// OPC UA PubSub standard format as specified in Part 14 of the OPC UA spec.
        /// Includes complete message metadata and supports all PubSub features.
        /// Default mode that provides best compatibility and completeness.
        /// Recommended for standards-compliant message processing.
        /// </summary>
        [EnumMember(Value = "PubSub")]
        PubSub,

        /// <summary>
        /// Simple JSON telemetry format compatible with Azure Time Series Insights.
        /// Each message contains basic node information and value changes.
        /// More compact than PubSub mode but with limited metadata.
        /// Suitable for simple monitoring scenarios.
        /// </summary>
        [EnumMember(Value = "Samples")]
        Samples,

        /// <summary>
        /// Complete OPC UA network message format including all headers and metadata.
        /// Provides maximum information about the message context and data.
        /// Largest message size but enables full featured message processing.
        /// Use when complete message context is required.
        /// </summary>
        [EnumMember(Value = "FullNetworkMessages")]
        FullNetworkMessages,

        /// <summary>
        /// Enhanced samples format with complete metadata and timestamps.
        /// Similar to Samples mode but includes additional context information.
        /// Provides good balance between completeness and message size.
        /// Suitable for advanced monitoring with historical context.
        /// </summary>
        [EnumMember(Value = "FullSamples")]
        FullSamples,

        /// <summary>
        /// Dataset messages without network message wrapper.
        /// Includes dataset metadata but omits network-level headers.
        /// Reduced message size while maintaining dataset context.
        /// Good choice when network-level information is not needed.
        /// </summary>
        [EnumMember(Value = "DataSetMessages")]
        DataSetMessages,

        /// <summary>
        /// Individual dataset message without network message wrapper.
        /// Similar to DataSetMessages but optimized for single dataset scenarios.
        /// More efficient than batched messages when publishing single datasets.
        /// Best for simple publisher configurations with one dataset.
        /// </summary>
        [EnumMember(Value = "SingleDataSetMessage")]
        SingleDataSetMessage,

        /// <summary>
        /// Pure key-value pairs representing datasets without any headers.
        /// Maximum efficiency with minimal overhead.
        /// Requires consumers to understand data context from configuration.
        /// Use when minimal message size is critical and context is known.
        /// </summary>
        [EnumMember(Value = "DataSets")]
        DataSets,

        /// <summary>
        /// Single dataset as key-value pairs without headers.
        /// Most efficient format for single dataset scenarios.
        /// Like DataSets mode but optimized for single dataset publishing.
        /// Best for minimal overhead in simple configurations.
        /// </summary>
        [EnumMember(Value = "SingleDataSet")]
        SingleDataSet,

        /// <summary>
        /// Datasets containing only raw values without type information.
        /// Most compact representation of data values.
        /// May lose OPC UA type information in transmission.
        /// Use only when data types are known by consumers.
        /// </summary>
        [EnumMember(Value = "RawDataSets")]
        RawDataSets,

        /// <summary>
        /// Single dataset with raw values only.
        /// Most compact format possible for single dataset.
        /// Combines benefits of SingleDataSet and RawDataSets modes.
        /// Best choice when absolute minimum message size is required.
        /// </summary>
        [EnumMember(Value = "SingleRawDataSet")]
        SingleRawDataSet
    }
}
