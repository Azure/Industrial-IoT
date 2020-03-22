// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum field
    /// </summary>
    [DataContract]
    public class EnumFieldApiModel {

        /// <summary>
        /// Name of the field
        /// </summary>
        [DataMember(Name = "name")]
        public string Name { get; set; }

        /// <summary>
        /// The value of the field.
        /// </summary>
        [DataMember(Name = "value")]
        public long Value { get; set; }

        /// <summary>
        /// Human readable name for the value.
        /// </summary>
        [DataMember(Name = "displayName",
            EmitDefaultValue = false)]
        public LocalizedTextApiModel DisplayName { get; set; }

        /// <summary>
        /// A description of the value.
        /// </summary>
        [DataMember(Name = "description",
            EmitDefaultValue = false)]
        public LocalizedTextApiModel Description { get; set; }
    }
}
