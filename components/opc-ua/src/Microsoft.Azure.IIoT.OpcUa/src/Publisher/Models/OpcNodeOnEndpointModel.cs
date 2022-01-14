// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    using System;
    using Newtonsoft.Json;
    using System.Text;
    using System.Runtime.Serialization;

    /// <summary>
    /// Class describing a list of nodes
    /// </summary>
    [DataContract]
    public class OpcNodeOnEndpointModel {

        /// <summary> Node Identifier </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string Id { get; set; }

        /// <summary> Expanded Node identifier </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string ExpandedNodeId { get; set; }

        /// <summary> Sampling interval </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int? OpcSamplingInterval { get; set; }

        /// <summary> Publishing interval </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int? OpcPublishingInterval { get; set; }

        /// <summary> Display name </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public string DisplayName { get; set; }

        /// <summary> Heartbeat </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public int? HeartbeatInterval { get; set; }

        /// <summary> Queue Size for the monitored item </summary>
        [DataMember(EmitDefaultValue = false, IsRequired = false)]
        public bool? SkipFirst { get; set; }
    }
}
