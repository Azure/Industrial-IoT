// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
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
        [EnumMember(Value = "Both")]
        Both,

        /// <summary>
        /// Source time
        /// </summary>
        [EnumMember(Value = "Source")]
        Source,

        /// <summary>
        /// Server time
        /// </summary>
        [EnumMember(Value = "Server")]
        Server,

        /// <summary>
        /// No timestamp
        /// </summary>
        [EnumMember(Value = "None")]
        None
    }
}
