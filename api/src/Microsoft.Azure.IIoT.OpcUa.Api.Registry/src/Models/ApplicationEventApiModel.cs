﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Registry.Models {
    using Newtonsoft.Json;

    /// <summary>
    /// Application event
    /// </summary>
    public class ApplicationEventApiModel {

        /// <summary>
        /// Event type
        /// </summary>
        [JsonProperty(PropertyName = "eventType")]
        public ApplicationEventType EventType { get; set; }

        /// <summary>
        /// Application
        /// </summary>
        [JsonProperty(PropertyName = "application",
            NullValueHandling = NullValueHandling.Ignore)]
        public ApplicationInfoApiModel Application { get; set; }
    }
}