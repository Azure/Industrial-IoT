// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using System.Runtime.Serialization;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Demand model
    /// </summary>
    [DataContract]
    public class DemandApiModel {

        /// <summary>
        /// Key
        /// </summary>
        [DataMember(Name = "key", Order = 0)]
        [Required]
        public string Key { get; set; }

        /// <summary>
        /// Match operator (defaults to equals)
        /// </summary>
        [DataMember(Name = "operator", Order = 1,
            EmitDefaultValue = false)]
        public DemandOperators? Operator { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [DataMember(Name = "value", Order = 2,
            EmitDefaultValue = false)]
        public string Value { get; set; }
    }
}