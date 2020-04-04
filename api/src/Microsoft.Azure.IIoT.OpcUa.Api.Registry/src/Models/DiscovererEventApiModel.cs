// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Discoverer event
    /// </summary>
    [DataContract]
    public class DiscovererEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        [DataMember(Name = "eventType", Order = 0)]
        public DiscovererEventType EventType { get; set; }

        /// <summary>
        /// Discoverer id
        /// </summary>
        [DataMember(Name = "id", Order = 1,
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Discoverer
        /// </summary>
        [DataMember(Name = "discoverer", Order = 2,
            EmitDefaultValue = false)]
        public DiscovererApiModel Discoverer { get; set; }
    }
}