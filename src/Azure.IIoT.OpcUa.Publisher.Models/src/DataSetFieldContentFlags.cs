// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Content for dataset field
    /// </summary>
    [Flags]
    [DataContract]
    public enum DataSetFieldContentFlags
    {
        /// <summary>
        /// Status code
        /// </summary>
        [EnumMember(Value = "StatusCode")]
        StatusCode = 0x1,

        /// <summary>
        /// Source timestamp
        /// </summary>
        [EnumMember(Value = "SourceTimestamp")]
        SourceTimestamp = 0x2,

        /// <summary>
        /// Server timestamp
        /// </summary>
        [EnumMember(Value = "ServerTimestamp")]
        ServerTimestamp = 0x4,

        /// <summary>
        /// Source picoseconds
        /// </summary>
        [EnumMember(Value = "SourcePicoSeconds")]
        SourcePicoSeconds = 0x8,

        /// <summary>
        /// Server picoseconds
        /// </summary>
        [EnumMember(Value = "ServerPicoSeconds")]
        ServerPicoSeconds = 0x10,

        /// <summary>
        /// Raw value
        /// </summary>
        [EnumMember(Value = "RawData")]
        RawData = 0x20,

        /// <summary>
        /// Write data set with one entry as value
        /// </summary>
        [EnumMember(Value = "SingleFieldDegradeToValue")]
        SingleFieldDegradeToValue = 0x1000,

        /// <summary>
        /// Node id included
        /// </summary>
        [EnumMember(Value = "NodeId")]
        NodeId = 0x10000,

        /// <summary>
        /// Display name included
        /// </summary>
        [EnumMember(Value = "DisplayName")]
        DisplayName = 0x20000,

        /// <summary>
        /// Endpoint url included
        /// </summary>
        [EnumMember(Value = "EndpointUrl")]
        EndpointUrl = 0x40000,

        /// <summary>
        /// Application uri
        /// </summary>
        [EnumMember(Value = "ApplicationUri")]
        ApplicationUri = 0x80000,

        /// <summary>
        /// Subscription id included
        /// </summary>
        [EnumMember(Value = "SubscriptionId")]
        SubscriptionId = 0x100000,

        /// <summary>
        /// Extension fields included
        /// </summary>
        [EnumMember(Value = "ExtensionFields")]
        ExtensionFields = 0x200000,

        /// <summary>
        /// Heartbeat indicator
        /// </summary>
        [EnumMember(Value = "Heartbeat")]
        Heartbeat = 0x400000
    }
}
