// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Endpoint Event model
    /// </summary>
    [DataContract]
    public class EndpointEventApiModel {

        /// <summary>
        /// Type of event
        /// </summary>
        [DataMember(Name = "eventType")]
        public EndpointEventType EventType { get; set; }

        /// <summary>
        /// Endpoint id
        /// </summary>
        [DataMember(Name = "id",
            EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// Endpoint info
        /// </summary>
        [DataMember(Name = "endpoint",
            EmitDefaultValue = false)]
        public EndpointInfoApiModel Endpoint { get; set; }
    }
}