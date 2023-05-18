// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Application event type
    /// </summary>
    [DataContract]
    public enum ApplicationEventType
    {
        /// <summary>
        /// New
        /// </summary>
        [EnumMember(Value = "New")]
        New,

        /// <summary>
        /// Enabled
        /// </summary>
        [EnumMember(Value = "Enabled")]
        Enabled,

        /// <summary>
        /// Disabled
        /// </summary>
        [EnumMember(Value = "Disabled")]
        Disabled,

        /// <summary>
        /// Updated
        /// </summary>
        [EnumMember(Value = "Updated")]
        Updated,

        /// <summary>
        /// Deleted
        /// </summary>
        [EnumMember(Value = "Deleted")]
        Deleted,
    }
}
