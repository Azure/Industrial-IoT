// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Represents an OPC UA Node identifier in string format.
    /// Used to identify nodes in the OPC UA address space for monitoring.
    /// Supports standard OPC UA node ID formats including:
    /// - Namespace index and identifier (ns=0;i=85)
    /// - String identifiers (ns=2;s=MyNode)
    /// - GUID identifiers (ns=3;g=8599E6C4-6667-4FB7-9EA9-C6896B31DB02)
    /// - Opaque/binary identifiers (ns=4;b=FA34E...)
    /// </summary>
    [DataContract]
    public sealed record class NodeIdModel
    {
        /// <summary>
        /// The node identifier string in standard OPC UA notation.
        /// Format: ns={namespace};{type}={value}
        /// Examples:
        /// - ns=0;i=85 (numeric identifier)
        /// - ns=2;s=MyNode (string identifier)
        /// - ns=3;g=8599E6C4-6667-4FB7-9EA9-C6896B31DB02 (GUID)
        /// - ns=4;b=FA34E... (binary/opaque)
        /// If namespace index is omitted, ns=0 is assumed.
        /// </summary>
        [DataMember(Name = "Identifier", Order = 0,
            EmitDefaultValue = false)]
        public string? Identifier { get; set; }
    }
}
