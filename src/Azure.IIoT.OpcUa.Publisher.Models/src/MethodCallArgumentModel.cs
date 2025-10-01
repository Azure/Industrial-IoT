// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using Furly.Extensions.Serializers;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;

    /// <summary>
    /// Method argument model
    /// </summary>
    [DataContract]
    public sealed record class MethodCallArgumentModel
    {
        /// <summary>
        /// Initial value or value to use
        /// </summary>
        [DataMember(Name = "value", Order = 0)]
        [SkipValidation]
        public VariantValue? Value { get; set; }

        /// <summary>
        /// Data type Id of the value (from meta data)
        /// </summary>
        [DataMember(Name = "dataType", Order = 1)]
        public string? DataType { get; set; }
    }
}
