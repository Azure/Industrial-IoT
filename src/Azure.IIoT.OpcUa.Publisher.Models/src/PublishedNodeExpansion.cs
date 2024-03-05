// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Node flags
    /// </summary>
    [DataContract]
    [Flags]
    public enum PublishedNodeExpansion
    {
        /// <summary>
        /// None
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0x0,

        /// <summary>
        /// The item should be expanded.
        /// </summary>
        Expand = 0x1,

        /// <summary>
        /// The item is an object and should
        /// be expanded into its variables.
        /// </summary>
        [EnumMember(Value = "OneLevel")]
        OneLevel = Expand,

        /// <summary>
        /// The item should be recursively
        /// expanded
        /// </summary>
        Recursive = 0x2,

        /// <summary>
        /// Expand item
        /// </summary>
        [EnumMember(Value = "MultiLevel")]
        MultiLevel = Recursive | Expand,
    }
}
