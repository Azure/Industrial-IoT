// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    using System.Text.Json.Nodes;

    /// <summary>
    /// Attribute value read
    /// </summary>
    [DataContract]
    public sealed record class AttributeReadResponseModel
    {
        /// <summary>
        /// Attribute value
        /// </summary>
        [DataMember(Name = "value", Order = 0)]
        [SkipValidation]
        public required JsonNode Value { get; set; }

        /// <summary>
        /// Service result in case of error
        /// </summary>
        [DataMember(Name = "errorInfo", Order = 1,
            EmitDefaultValue = false)]
        public ServiceResultModel? ErrorInfo { get; set; }
    }
}
