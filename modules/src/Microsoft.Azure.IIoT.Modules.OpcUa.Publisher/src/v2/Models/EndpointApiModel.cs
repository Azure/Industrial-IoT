// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint model for edgeservice api
    /// </summary>
    public class EndpointApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointApiModel(EndpointModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Url = model.Url;
            AlternativeUrls = model.AlternativeUrls;
            Certificate = model.Certificate;
            SecurityMode = model.SecurityMode;
            SecurityPolicy = model.SecurityPolicy;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public EndpointModel ToServiceModel() {
            return new EndpointModel {
                Url = Url,
                AlternativeUrls = AlternativeUrls,
                Certificate = Certificate,
                SecurityMode = SecurityMode,
                SecurityPolicy = SecurityPolicy,
            };
        }

        /// <summary>
        /// Endpoint url to use to connect with
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        /// <summary>
        /// Alternative endpoint urls that can be used for
        /// accessing and validating the server
        /// </summary>
        [JsonProperty(PropertyName = "alternativeUrls",
            NullValueHandling = NullValueHandling.Ignore)]
        public HashSet<string> AlternativeUrls { get; set; }

        /// <summary>
        /// Security Mode to use for communication
        /// default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityMode? SecurityMode { get; set; }

        /// <summary>
        /// Security policy uri to use for communication
        /// default to best.
        /// </summary>
        [JsonProperty(PropertyName = "securityPolicy",
            NullValueHandling = NullValueHandling.Ignore)]
        public string SecurityPolicy { get; set; }

        /// <summary>
        /// Certificate thumbprint to validate against or
        /// null to trust none.
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Certificate { get; set; }
    }
}
