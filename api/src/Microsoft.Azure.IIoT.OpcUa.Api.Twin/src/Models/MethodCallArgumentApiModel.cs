// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System.Runtime.Serialization;

    /// <summary>
    /// method arg model
    /// </summary>
    [DataContract]
    public class MethodCallArgumentApiModel {

        /// <summary>
        /// Initial value or value to use
        /// </summary>
        [DataMember(Name = "value", Order = 0)]
        public VariantValue Value { get; set; }

        /// <summary>
        /// Data type Id of the value (from meta data)
        /// </summary>
        [DataMember(Name = "dataType", Order = 1)]
        public string DataType { get; set; }
    }
}
