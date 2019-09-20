// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Endpoint Activation Filter model
    /// </summary>
    public class EndpointActivationFilterApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public EndpointActivationFilterApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public EndpointActivationFilterApiModel(EndpointActivationFilterModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            SecurityPolicies = model.SecurityPolicies;
            SecurityPolicies = model.SecurityPolicies;
            SecurityMode = model.SecurityMode;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public EndpointActivationFilterModel ToServiceModel() {
            return new EndpointActivationFilterModel {
                TrustLists = TrustLists,
                SecurityPolicies = SecurityPolicies,
                SecurityMode = SecurityMode
            };
        }

        /// <summary>
        /// Certificate trust list identifiers to use for
        /// activation, if null, all certificates are
        /// trusted.  If empty list, no certificates are
        /// trusted which is equal to no filter.
        /// </summary>
        [JsonProperty(PropertyName = "TrustLists",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> TrustLists { get; set; }

        /// <summary>
        /// Endpoint security policies to filter against.
        /// If set to null, all policies are in scope.
        /// </summary>
        [JsonProperty(PropertyName = "SecurityPolicies",
           NullValueHandling = NullValueHandling.Ignore)]
        public List<string> SecurityPolicies { get; set; }

        /// <summary>
        /// Security mode level to activate. If null,
        /// then <see cref="SecurityMode.Best"/> is assumed.
        /// </summary>
        [JsonProperty(PropertyName = "SecurityMode",
            NullValueHandling = NullValueHandling.Ignore)]
        public SecurityMode? SecurityMode { get; set; }
    }
}

