// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Method metadata query model
    /// </summary>
    [DataContract]
    public class MethodMetadataResponseApiModel {

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
        public List<MethodMetadataArgumentApiModel> InputArguments { get; set; }

        /// <summary>
        /// output argument meta data
        /// </summary>
        [DataMember(Name = "outputArguments", Order = 2,
            EmitDefaultValue = false)]
        public List<MethodMetadataArgumentApiModel> OutputArguments { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 3,
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
