// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// A monitored and published item
    /// </summary>
    [DataContract]
    public class PublishNodesRequestApiModel {

        /// <summary> The Group the stream belongs to - DataSetWriterGroup. </summary>
        [DataMember(Name = "dataSetWriterGroup", Order = 0,
            EmitDefaultValue = false)]
        public string DataSetWriterGroup { get; set; }

        /// <summary> Id Identifier of the DataFlow - DataSetWriterId. </summary>
        [DataMember(Name = "dataSetWriterId", Order = 1,
            EmitDefaultValue = false)]
        public string DataSetWriterId { get; set; }

        /// <summary> The Publishing interval for a dataset writer </summary>
        [DataMember(Name = "dataSetPublishingInterval", Order = 2,
            EmitDefaultValue = false)]
        public int? DataSetPublishingInterval { get; set; }

        /// <summary> Endpoint URL for the OPC Nodes to monitor </summary>
        [DataMember(Name = "endpointUrl", Order = 3)]
        [Required]
        public string EndpointUrl { get; set; }

        /// <summary> Use a secured channel for the opc ua communication </summary>
        [DataMember(Name = "useSecurity", Order = 4,
            EmitDefaultValue = false)]
        public bool UseSecurity { get; set; }

        /// <summary> endpoint authentication mode </summary>
        [DataMember(Name = "opcAuthenticationMode", Order = 5,
            EmitDefaultValue = false)]
        public AuthenticationMode OpcAuthenticationMode { get; set; }

        /// <summary> Endpoint's username </summary>
        [DataMember(Name = "userName", Order = 6,
            EmitDefaultValue = false)]
        public string UserName { get; set; }

        /// <summary> endpoint password </summary>
        [DataMember(Name = "password", Order = 7,
            EmitDefaultValue = false)]
        public string Password { get; set; }

        /// <summary> List of the OpcNodes to be monitored </summary>
        [DataMember(Name = "opcNodes", Order = 8,
            EmitDefaultValue = false)]
        public List<PublishedNodeApiModel> OpcNodes { get; set; }
    }
}
