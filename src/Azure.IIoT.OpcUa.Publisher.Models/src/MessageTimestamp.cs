// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Message timestamp configuration option
    /// </summary>
    [DataContract]
    public enum MessageTimestamp
    {
        /// <summary>
        /// The time (utc) the message was created either because
        /// it was received from the server or as heartbeat, using
        /// the publisher module clock.
        /// </summary>
        [EnumMember(Value = "CurrentTimeUtc")]
        CurrentTimeUtc,

        /// <summary>
        /// The publish time of the subscription notification
        /// which is using the server clock. None if not provided
        /// or available.
        /// </summary>
        [EnumMember(Value = "PublishTime")]
        PublishTime,

        /// <summary>
        /// The time (utc) the message was encoded and subsequently
        /// put on the wire, using the publisher module clock.
        /// </summary>
        [EnumMember(Value = "EncodingTimeUtc")]
        EncodingTimeUtc,
    }
}
