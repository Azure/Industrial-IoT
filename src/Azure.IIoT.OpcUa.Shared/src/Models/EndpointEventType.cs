// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Activation state of the endpoint twin
    /// </summary>
    [DataContract]
    public enum EndpointEventType
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
        /// Deleted
        /// </summary>
        [EnumMember]
        Deleted,
    }
}
