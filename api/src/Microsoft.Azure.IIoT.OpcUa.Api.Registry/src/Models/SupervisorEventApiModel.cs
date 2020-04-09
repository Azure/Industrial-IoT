// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Supervisor event
    /// </summary>
    [DataContract]
    public class SupervisorEventApiModel {

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
        public string Id { get; set; }

        /// <summary>
        /// Application
        /// </summary>
        [DataMember(Name = "supervisor", Order = 2,
            EmitDefaultValue = false)]
        public SupervisorApiModel Supervisor { get; set; }
    }
}