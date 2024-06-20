// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System;
    using System.Runtime.Serialization;

    /// <summary>
    /// Model for reporting runtime state.
    /// </summary>
    [DataContract]
    public class RuntimeStateEventModel
    {
        /// <summary>
        /// Defines the message type that is sent.
        /// </summary>
        [DataMember(Name = "MessageType", Order = 0,
            EmitDefaultValue = true)]
        public RuntimeStateEventType MessageType { get; set; }

        /// <summary>
        /// Defines the message version.
        /// </summary>
        [DataMember(Name = "MessageVersion", Order = 1,
            EmitDefaultValue = true)]
        public int MessageVersion { get; set; }

        /// <summary>
        /// The utc timestamp of the runtime state event
        /// </summary>
        [DataMember(Name = "TimestampUtc", Order = 2,
            EmitDefaultValue = true)]
        public DateTimeOffset TimestampUtc { get; set; }

        /// <summary>
        /// The Publisher version
        /// </summary>
        [DataMember(Name = "Version", Order = 3,
            EmitDefaultValue = true)]
        public string? Version { get; set; }

        /// <summary>
        /// The Publisher Id if available
        /// </summary>
        [DataMember(Name = "PublisherId", Order = 4,
            EmitDefaultValue = true)]
        public string? PublisherId { get; set; }

        /// <summary>
        /// The Site if available
        /// </summary>
        [DataMember(Name = "Site", Order = 5,
            EmitDefaultValue = true)]
        public string? Site { get; set; }

        /// <summary>
        /// The Device Id if available
        /// </summary>
        [DataMember(Name = "DeviceId", Order = 6,
            EmitDefaultValue = true)]
        public string? DeviceId { get; set; }

        /// <summary>
        /// The Module Id if available
        /// </summary>
        [DataMember(Name = "ModuleId", Order = 7,
            EmitDefaultValue = true)]
        public string? ModuleId { get; set; }

        /// <summary>
        /// The Publisher semantic version string
        /// </summary>
        [DataMember(Name = "SemVer", Order = 8,
            EmitDefaultValue = true)]
        public string? SemVer { get; set; }
    }
}
