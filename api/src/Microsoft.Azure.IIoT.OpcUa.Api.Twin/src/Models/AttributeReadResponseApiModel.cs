// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// Attribute value read
    /// </summary>
    [DataContract]
    public class AttributeReadResponseApiModel {

        /// <summary>
        /// Attribute value
        /// </summary>
        [DataMember(Name = "value", Order = 0)]
        public VariantValue Value { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 1,
            EmitDefaultValue = false)]
        public ServiceResultApiModel ErrorInfo { get; set; }
    }
}
