// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Discovery request
    /// </summary>
    public class DiscoveryRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiscoveryRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DiscoveryRequestApiModel(DiscoveryRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Id = model.Id;
            Discovery = model.Discovery;
            Configuration = model.Configuration == null ? null :
                new DiscoveryConfigApiModel(model.Configuration);
            Context = model.Context == null ? null :
                new RegistryOperationContextApiModel(model.Context);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public DiscoveryRequestModel ToServiceModel() {
            return new DiscoveryRequestModel {
                Id = Id,
                Configuration = Configuration?.ToServiceModel(),
                Discovery = Discovery,
                Context = Context?.ToServiceModel()
            };
        }

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [JsonProperty(PropertyName = "Id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Discovery mode to use
        /// </summary>
        [JsonProperty(PropertyName = "Discovery",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiscoveryMode? Discovery { get; set; }

        /// <summary>
        /// Scan configuration to use
        /// </summary>
        [JsonProperty(PropertyName = "Configuration",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiscoveryConfigApiModel Configuration { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [JsonProperty(PropertyName = "context",
            NullValueHandling = NullValueHandling.Ignore)]
        public RegistryOperationContextApiModel Context { get; set; }
    }
}
