// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Direction to browse
    /// </summary>
    [DataContract]
    public enum BrowseDirection
    {
        /// <summary>
        /// Browse forward (default)
        /// </summary>
        [EnumMember(Value = "Forward")]
        Forward,

        /// <summary>
        /// Browse backward
        /// </summary>
        [EnumMember(Value = "Backward")]
        Backward,

        /// <summary>
        /// Browse both directions
        /// </summary>
        [EnumMember(Value = "Both")]
        Both
    }
}
