// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Activation state of the endpoint twin
    /// </summary>
    [DataContract]
    public enum EndpointEventType {

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
        /// Deactivated
        /// </summary>
        [EnumMember]
        Deactivated,

        /// <summary>
        /// Activated
        /// </summary>
        [EnumMember]
        Activated,

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