// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Desired writer group transport
    /// </summary>
    [DataContract]
    public enum WriterGroupTransport
    {
        /// <summary>
        /// IoT Hub or IoT Edge
        /// </summary>
        [EnumMember(Value = "IoTHub")]
        IoTHub,

        /// <summary>
        /// Mqtt broker
        /// </summary>
        [EnumMember(Value = "Mqtt")]
        Mqtt,

        /// <summary>
        /// Dapr
        /// </summary>
        [EnumMember(Value = "Dapr")]
        Dapr,

        /// <summary>
        /// Http web hook
        /// </summary>
        [EnumMember(Value = "Http")]
        Http,

        /// <summary>
        /// File system
        /// </summary>
        [EnumMember(Value = "FileSystem")]
        FileSystem,

        /// <summary>
        /// Null
        /// </summary>
        [EnumMember(Value = "Null")]
        Null
    }
}
