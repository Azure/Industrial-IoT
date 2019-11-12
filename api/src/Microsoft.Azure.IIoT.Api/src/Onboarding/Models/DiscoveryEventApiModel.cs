// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Onboarding.Models {
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Discovery event
    /// </summary>
    public class DiscoveryEventApiModel {

        /// <summary>
        /// Timestamp of the discovery sweep.
        /// </summary>
        [JsonProperty(PropertyName = "timeStamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Index in the batch with same timestamp.
        /// </summary>
        [JsonProperty(PropertyName = "index")]
        public int Index { get; set; }

        /// <summary>
        /// Discovered endpoint in form of endpoint registration
        /// </summary>
        [JsonProperty(PropertyName = "registration")]
        public EndpointRegistrationApiModel Registration { get; set; }

        /// <summary>
        /// Application to which this endpoint belongs
        /// </summary>
        [JsonProperty(PropertyName = "application")]
        public ApplicationInfoApiModel Application { get; set; }
    }
}