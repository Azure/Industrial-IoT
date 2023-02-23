﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Gateway event type
    /// </summary>
    [DataContract]
    public enum GatewayEventType
    {
        /// <summary>
        /// New
        /// </summary>
        [EnumMember]
        New,

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
