// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Gateway event
    /// </summary>
    [DataContract]
    public class GatewayEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        [DataMember(Name = "eventType", Order = 0)]
        public GatewayEventType EventType { get; set; }

        /// <summary>
        /// Gateway id
        /// </summary>
        [DataMember(Name = "id", Order = 1,
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Gateway
        /// </summary>
        [DataMember(Name = "gateway", Order = 2,
            EmitDefaultValue = false)]
        public GatewayApiModel Gateway { get; set; }
    }
}