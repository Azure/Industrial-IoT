// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Enum of valid values for MessageType.
    /// </summary>
    [DataContract]
    public enum RuntimeStateEventType
    {
        /// <summary>
        /// Defines a message of restart announcement type.
        /// </summary>
        [EnumMember]
        RestartAnnouncement
    }
}
