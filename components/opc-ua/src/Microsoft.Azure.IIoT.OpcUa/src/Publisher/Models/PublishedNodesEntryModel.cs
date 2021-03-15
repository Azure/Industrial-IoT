// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Data;
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Config.Models.Events;
    using System;
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Contains the nodes which should be
    /// </summary>
    [DataContract]
    public class PublishedNodesEntryModel {

        /// <summary> Id Identifier of the DataFlow - DataSetWriterId. </summary>
        [DataMember(IsRequired = false)]
        public string DataSetWriterId { get; set; }

        /// <summary> The Group the stream belongs to - DataSetWriterGroup. </summary>
        [DataMember(IsRequired = false)]
        public string DataSetWriterGroup { get; set; }

        /// <summary> The Publishing interval for a dataset writer </summary>
        [DataMember(IsRequired = false)]
        public int? DataSetPublishingInterval { get; set; }

        /// <summary> The endpoint URL of the OPC UA server. </summary>
        [DataMember(IsRequired = true)]
        public Uri EndpointUrl { get; set; }

        /// <summary> Secure transport should be used to </summary>
        [DataMember(EmitDefaultValue = false)]
        public bool? UseSecurity { get; set; }

        /// <summary> The node to monitor in "ns=" syntax. </summary>
        [DataMember(EmitDefaultValue = false)]
        public NodeIdModel NodeId { get; set; }

        /// <summary> authentication mode </summary>
        [DataMember(EmitDefaultValue = false)]
        public OpcAuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary> encrypted username </summary>
        [DataMember(EmitDefaultValue = false)]
        public string EncryptedAuthUsername { get; set; }

        /// <summary> encrypted password </summary>
        [DataMember]
        public string EncryptedAuthPassword { get; set; }

        /// <summary> plain username </summary>
        [DataMember(EmitDefaultValue = false)]
        public string OpcAuthenticationUsername { get; set; }

        /// <summary> plain password </summary>
        [DataMember]
        public string OpcAuthenticationPassword { get; set; }

        /// <summary> Data Nodes defined in the collection. </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<OpcDataNodeModel> OpcNodes { get; set; }

        /// <summary> Event Nodes defined in the collection. </summary>
        [DataMember(EmitDefaultValue = false)]
        public List<OpcEventNodeModel> OpcEvents { get; set; }
    }
}
