// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Registry.Onboarding.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;

    /// <summary>
    /// Discovery result model
    /// </summary>
    public class DiscoveryResultApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiscoveryResultApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DiscoveryResultApiModel(DiscoveryResultModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Id = model.Id;
            DiscoveryConfig = model.DiscoveryConfig == null ? null :
                new DiscoveryConfigApiModel(model.DiscoveryConfig);
            Context = model.Context == null ? null :
                new RegistryOperationApiModel(model.Context);
            Diagnostics = model.Diagnostics;
            RegisterOnly = model.RegisterOnly;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public DiscoveryResultModel ToServiceModel() {
            return new DiscoveryResultModel {
                Id = Id,
                DiscoveryConfig = DiscoveryConfig?.ToServiceModel(),
                Context = Context?.ToServiceModel(),
                Diagnostics = Diagnostics,
                RegisterOnly = RegisterOnly
            };
        }

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Configuration used during discovery
        /// </summary>
        [JsonProperty(PropertyName = "discoveryConfig",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiscoveryConfigApiModel DiscoveryConfig { get; set; }

        /// <summary>
        /// If true, only register, do not unregister based
        /// on these events.
        /// </summary>
        [JsonProperty(PropertyName = "registerOnly",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? RegisterOnly { get; set; }

        /// <summary>
        /// If discovery failed, result information
        /// </summary>
        [JsonProperty(PropertyName = "diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public JToken Diagnostics { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [JsonProperty(PropertyName = "context",
            NullValueHandling = NullValueHandling.Ignore)]
        public RegistryOperationApiModel Context { get; set; }
    }
}