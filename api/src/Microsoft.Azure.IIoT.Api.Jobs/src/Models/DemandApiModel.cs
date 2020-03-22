﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Api.Jobs.Models {
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
        [DataMember(Name = "key")]
        [Required]
        public string Key { get; set; }

        /// <summary>
        /// Match operator (defaults to equals)
        /// </summary>
        [DataMember(Name = "operator",
            EmitDefaultValue = false)]
        public DemandOperators? Operator { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [DataMember(Name = "value",
            EmitDefaultValue = false)]
        public string Value { get; set; }
    }
}