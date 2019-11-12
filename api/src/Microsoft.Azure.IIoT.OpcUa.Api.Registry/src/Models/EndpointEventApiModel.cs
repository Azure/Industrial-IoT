// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Activation state of the endpoint twin
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum EndpointEventType {

        /// <summary>
        /// New
        /// </summary>
        New,

        /// <summary>
        /// Enabled
        /// </summary>
        Enabled,

        /// <summary>
        /// Disabled
        /// </summary>
        Disabled,

        /// <summary>
        /// Deactivated
        /// </summary>
        Deactivated,

        /// <summary>
        /// Activated
        /// </summary>
        Activated,

        /// <summary>
        /// Updated
        /// </summary>
        Updated,

        /// <summary>
        /// Deleted
        /// </summary>
        Deleted,
    }

    /// <summary>
    /// Endpoint Event model
    /// </summary>
    public class EndpointEventApiModel {

        /// <summary>
        /// Type of event
        /// </summary>
        [JsonProperty(PropertyName = "eventType")]
        public EndpointEventType EventType { get; set; }

        /// <summary>
        /// Endpoint info
        /// </summary>
        [JsonProperty(PropertyName = "endpoint",
            NullValueHandling = NullValueHandling.Ignore)]
        public EndpointInfoApiModel Endpoint { get; set; }
    }
}