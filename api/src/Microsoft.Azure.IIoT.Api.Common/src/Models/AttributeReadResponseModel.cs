// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// Attribute value read
    /// </summary>
    [DataContract]
    public record class AttributeReadResponseModel {

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
        public ServiceResultModel ErrorInfo { get; set; }
    }
}
