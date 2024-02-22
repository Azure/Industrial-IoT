// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Data set routing
    /// </summary>
    [DataContract]
    public enum DataSetRoutingMode
    {
        /// <summary>
        /// Use custom topic paths for all elements
        /// </summary>
        [EnumMember(Value = "None")]
        None = 0,

        /// <summary>
        /// Use browse names as topic path elements
        /// unless otherwise configured
        /// </summary>
        [EnumMember(Value = "UseBrowseNames")]
        UseBrowseNames = 1,

        /// <summary>
        /// Use browse names with namespace index
        /// as path elements unless otherwise configured
        /// </summary>
        [EnumMember(Value = "UseBrowseNamesWithNamespaceIndex")]
        UseBrowseNamesWithNamespaceIndex = 2,
    }
}
