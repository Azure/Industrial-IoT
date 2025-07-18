// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Namespace serialization format for node ids
    /// and qualified names.
    /// </summary>
    [DataContract]
    public enum NamespaceFormat
    {
        /// <summary>
        /// The legacy uri format
        /// </summary>
        [EnumMember(Value = "Uri")]
        Uri,

        /// <summary>
        /// With ns= namespace index except for index 0
        /// </summary>
        [EnumMember(Value = "Index")]
        Index,

        /// <summary>
        /// With nsu= namespace except for namespace 0
        /// </summary>
        [EnumMember(Value = "Expanded")]
        Expanded,

        /// <summary>
        /// With nsu= namespace even for namespace 0
        /// </summary>
        [EnumMember(Value = "ExpandedWithNamespace0")]
        ExpandedWithNamespace0,
    }
}
