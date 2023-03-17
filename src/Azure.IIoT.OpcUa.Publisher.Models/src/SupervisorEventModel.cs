// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Publisher.Models
{
    using System.Runtime.Serialization;

    /// <summary>
    /// Supervisor event
    /// </summary>
    [DataContract]
    public sealed record class SupervisorEventModel
    {
        /// <summary>
        /// Event type
        /// </summary>
        [DataMember(Name = "eventType", Order = 0)]
        public SupervisorEventType EventType { get; set; }

        /// <summary>
        /// Supervisor id
        /// </summary>
        [DataMember(Name = "id", Order = 1,
            EmitDefaultValue = false)]
        public string? Id { get; set; }

        /// <summary>
        /// Application
        /// </summary>
        [DataMember(Name = "supervisor", Order = 2,
            EmitDefaultValue = false)]
        public SupervisorModel? Supervisor { get; set; }

        /// <summary>
        /// Context
        /// </summary>
        [DataMember(Name = "context", Order = 3,
            EmitDefaultValue = false)]
        public OperationContextModel? Context { get; set; }
    }
}
