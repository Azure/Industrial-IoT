// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Specifies how OPC UA node paths are mapped to message routing paths/topics.
    /// Controls automatic topic structure generation from OPC UA address space.
    /// Used to create a unified namespace when publishing to message brokers
    /// that support hierarchical routing like MQTT.
    /// </summary>
    [DataContract]
    public enum DataSetRoutingMode
    {
        /// <summary>
        /// Disable automatic topic path generation.
        /// Uses only explicitly configured topic templates and queue names.
        /// Provides maximum control over message routing.
        /// Best when custom routing patterns are required.
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0,

        /// <summary>
        /// Automatically generate topic paths using OPC UA browse names.
        /// Creates hierarchical paths matching the OPC UA address space structure.
        /// Makes data organization intuitive and discoverable.
        /// Example: "Objects/Server/Data/Value1"
        /// Falls back to configured templates if specified.
        /// </summary>
        [EnumMember(Value = "UseBrowseNames")]
        UseBrowseNames = 1,

        /// <summary>
        /// Generate topic paths using browse names prefixed with namespace index.
        /// Ensures unique paths when nodes have same names in different namespaces.
        /// Preserves complete namespace context in the routing path.
        /// Example: "0:Objects/2:MyDevice/2:Sensors/2:Temperature"
        /// Falls back to configured templates if specified.
        /// </summary>
        [EnumMember(Value = "UseBrowseNamesWithNamespaceIndex")]
        UseBrowseNamesWithNamespaceIndex = 2,
    }
}
