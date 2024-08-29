// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Endpoint Event model
    /// </summary>
    [DataContract]
    public sealed record class EndpointEventModel
    {
        /// <summary>
        /// Type of event
        /// </summary>
        [DataMember(Name = "eventType", Order = 0)]
        public EndpointEventType EventType { get; set; }

        /// <summary>
        /// Endpoint id
        /// </summary>
        [DataMember(Name = "id", Order = 1,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Endpoint info
        /// </summary>
        [DataMember(Name = "endpoint", Order = 2)]
        public required EndpointInfoModel Endpoint { get; set; }

        /// <summary>
        /// Context
        /// </summary>
        [DataMember(Name = "context", Order = 3,
            EmitDefaultValue = false)]
        public OperationContextModel? Context { get; set; }
    }
}
