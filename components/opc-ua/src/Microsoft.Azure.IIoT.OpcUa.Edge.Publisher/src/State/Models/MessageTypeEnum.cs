// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State.Models {

    using System.Runtime.Serialization;

    /// <summary>
    /// Enum of valid values for MessageType.
    /// </summary>
    [DataContract]
    public enum MessageTypeEnum {
        /// <summary> Defines a message of restart announcement type. </summary>
        [EnumMember]
        RestartAnnouncement
    }
}
