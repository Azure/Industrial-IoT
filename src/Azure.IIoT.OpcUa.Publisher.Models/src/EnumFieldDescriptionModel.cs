// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum description field
    /// </summary>
    [DataContract]
    public record class EnumFieldDescriptionModel
    {
        /// <summary>
        /// Name of the type
        /// </summary>
        [DataMember(Name = "name", Order = 1)]
        public required string Name { get; set; }

        /// <summary>
        /// Value of the field in the enum
        /// </summary>
        [DataMember(Name = "value", Order = 2)]
        public long Value { get; set; }

        /// <summary>
        /// Name to display to user
        /// </summary>
        [DataMember(Name = "displayName", Order = 3,
            EmitDefaultValue = false)]
        public string? DisplayName { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [DataMember(Name = "description", Order = 4,
            EmitDefaultValue = false)]
        public string? Description { get; set; }
    }
}
