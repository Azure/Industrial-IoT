// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// An element of a relative path
    /// </summary>
    [DataContract]
    public record class RelativePathElementModel
    {
        /// <summary>
        /// Target browse name with namespace
        /// </summary>
        [DataMember(Name = "TargetName", Order = 0)]
        public required string TargetName { get; init; }

        /// <summary>
        /// Reference type identifier.
        /// (default is hierarchical reference)
        /// </summary>
        [DataMember(Name = "ReferenceTypeId", Order = 1)]
        public required string ReferenceTypeId { get; init; }

        /// <summary>
        /// Whether the reference is inverse
        /// (default is false)
        /// </summary>
        [DataMember(Name = "IsInverse", Order = 2,
            EmitDefaultValue = false)]
        public bool? IsInverse { get; init; }

        /// <summary>
        /// Whether reference subtypes should be excluded
        /// (default is false)
        /// </summary>
        [DataMember(Name = "noSubtypes", Order = 3,
            EmitDefaultValue = false)]
        public bool? NoSubtypes { get; init; }
    }
}
