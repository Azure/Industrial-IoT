// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Direction to browse
    /// </summary>
    [DataContract]
    public enum BrowseDirection {

        /// <summary>
        /// Forward
        /// </summary>
        [EnumMember]
        Forward,

        /// <summary>
        /// Backward
        /// </summary>
        [EnumMember]
        Backward,

        /// <summary>
        /// Both directions
        /// </summary>
        [EnumMember]
        Both
    }
}
