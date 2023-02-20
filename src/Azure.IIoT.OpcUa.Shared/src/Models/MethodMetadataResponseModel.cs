// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Collections.Generic;
    using System.Runtime.Serialization;

    /// <summary>
    /// Result of method metadata query
    /// </summary>
    [DataContract]
    public record class MethodMetadataResponseModel {

        /// <summary>
        /// Id of object that the method is a component of
        /// </summary>
        [DataMember(Name = "objectId", Order = 0,
            EmitDefaultValue = false)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Input argument meta data
        /// </summary>
        [DataMember(Name = "inputArguments", Order = 1,
            EmitDefaultValue = false)]
        public List<MethodMetadataArgumentModel> InputArguments { get; set; }

        /// <summary>
        /// Output argument meta data
        /// </summary>
        [DataMember(Name = "outputArguments", Order = 2,
            EmitDefaultValue = false)]
        public List<MethodMetadataArgumentModel> OutputArguments { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 3,
            EmitDefaultValue = false)]
        public ServiceResultModel ErrorInfo { get; set; }
    }
}
