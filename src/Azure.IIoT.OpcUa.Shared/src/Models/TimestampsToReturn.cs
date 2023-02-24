// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Timestamps
    /// </summary>
    [DataContract]
    public enum TimestampsToReturn
    {
        /// <summary>
        /// Both time stamps
        /// </summary>
        [EnumMember]
        Both,

        /// <summary>
        /// Source time
        /// </summary>
        [EnumMember]
        Source,

        /// <summary>
        /// Server time
        /// </summary>
        [EnumMember]
        Server,

        /// <summary>
        /// No timestamp
        /// </summary>
        [EnumMember]
        None
    }
}
