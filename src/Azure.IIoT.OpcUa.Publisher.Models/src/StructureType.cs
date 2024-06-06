// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Type of structure
    /// </summary>
    [DataContract]
    public enum StructureType
    {
        /// <summary>
        /// Default
        /// </summary>
        [EnumMember(Value = "Structure")]
        Structure = 0,

        /// <summary>
        /// With optional fields
        /// </summary>
        [EnumMember(Value = "StructureWithOptionalFields")]
        StructureWithOptionalFields = 1,

        /// <summary>
        /// Union
        /// </summary>
        [EnumMember(Value = "Union")]
        Union = 2,

        /// <summary>
        /// With subtyped values
        /// </summary>
        [EnumMember(Value = "StructureWithSubtypedValues")]
        StructureWithSubtypedValues = 3,

        /// <summary>
        /// Union but with subtyped values
        /// </summary>
        [EnumMember(Value = "UnionWithSubtypedValues")]
        UnionWithSubtypedValues = 4,
    }
}
