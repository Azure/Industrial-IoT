// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Value write request model
    /// </summary>
    [DataContract]
    public class ValueWriteRequestApiModel {

        /// <summary>
        /// Node id to to write value to.
        /// </summary>
        [DataMember(Name = "nodeId", Order = 0,
            EmitDefaultValue = false)]
        public string NodeId { get; set; }

        /// <summary>
        /// An optional path from NodeId instance to
        /// the actual node.
        /// </summary>
        [DataMember(Name = "browsePath", Order = 1,
            EmitDefaultValue = false)]
        public string[] BrowsePath { get; set; }

        /// <summary>
        /// Value to write. The system tries to convert
        /// the value according to the data type value,
        /// e.g. convert comma seperated value strings
        /// into arrays.  (Mandatory)
        /// </summary>
        [DataMember(Name = "value", Order = 2)]
        [Required]
        public VariantValue Value { get; set; }

        /// <summary>
        /// A built in datatype for the value. This can
        /// be a data type from browse, or a built in
        /// type.
        /// (default: best effort)
        /// </summary>
        [DataMember(Name = "dataType", Order = 3,
            EmitDefaultValue = false)]
        public string DataType { get; set; }

        /// <summary>
        /// Index range to write
        /// </summary>
        [DataMember(Name = "indexRange", Order = 4,
            EmitDefaultValue = false)]
        public string IndexRange { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [DataMember(Name = "header", Order = 5,
            EmitDefaultValue = false)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
