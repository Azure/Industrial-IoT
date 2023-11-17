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
        /// Restart announcement.
        /// </summary>
        [EnumMember(Value = "RestartAnnouncement")]
        RestartAnnouncement,

        /// <summary>
        /// Runtime state is running
        /// </summary>
        [EnumMember(Value = "Running")]
        Running,

        /// <summary>
        /// Shutdown announcement.
        /// </summary>
        [EnumMember(Value = "ShutdownAnnouncement")]
        ShutdownAnnouncement,

        /// <summary>
        /// Runtime state is stopped
        /// </summary>
        [EnumMember(Value = "Stopped")]
        Stopped,
    }
}
