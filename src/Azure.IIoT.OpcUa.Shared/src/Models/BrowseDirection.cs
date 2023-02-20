// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Shared.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Direction to browse
    /// </summary>
    [DataContract]
    public enum BrowseDirection {

        /// <summary>
        /// Browse forward (default)
        /// </summary>
        [EnumMember]
        Forward,

        /// <summary>
        /// Browse backward
        /// </summary>
        [EnumMember]
        Backward,

        /// <summary>
        /// Browse both directions
        /// </summary>
        [EnumMember]
        Both
    }
}
