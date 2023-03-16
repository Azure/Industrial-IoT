// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Models
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
        [EnumMember]
        New,

        /// <summary>
        /// Enabled
        /// </summary>
        [EnumMember]
        Enabled,

        /// <summary>
        /// Disabled
        /// </summary>
        [EnumMember]
        Disabled,

        /// <summary>
        /// Updated
        /// </summary>
        [EnumMember]
        Updated,

        /// <summary>
        /// Deleted
        /// </summary>
        [EnumMember]
        Deleted,
    }
}
