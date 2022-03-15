// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Edge.Publisher.State.Models {

    using System.Runtime.Serialization;

    /// <summary>
    /// Model for reporting runtime state.
    /// </summary>
    public class RuntimeStateModel {

        /// <summary> Defines the message type that is sent. </summary>
        [DataMember(Name = "messageType", Order = 0, EmitDefaultValue = true)]
        public MessageTypeEnum MessageType { get; set; }

        /// <summary> Defines the message version. </summary>
        [DataMember(Name = "messageVersion", Order = 1, EmitDefaultValue = true)]
        public int MessageVersion { get; } = 1;
    }
}
