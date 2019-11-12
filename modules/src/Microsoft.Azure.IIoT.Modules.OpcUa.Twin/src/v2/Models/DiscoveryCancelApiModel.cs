// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Discovery cancel request
    /// </summary>
    public class DiscoveryCancelApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DiscoveryCancelApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public DiscoveryCancelApiModel(DiscoveryCancelModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Id = model.Id;
            Context = model.Context == null ? null :
                new RegistryOperationContextApiModel(model.Context);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public DiscoveryCancelModel ToServiceModel() {
            return new DiscoveryCancelModel {
                Id = Id,
                Context = Context?.ToServiceModel()
            };
        }

        /// <summary>
        /// Id of discovery request
        /// </summary>
        [JsonProperty(PropertyName = "id",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        /// <summary>
        /// Operation audit context
        /// </summary>
        [JsonProperty(PropertyName = "context",
            NullValueHandling = NullValueHandling.Ignore)]
        public RegistryOperationContextApiModel Context { get; set; }
    }
}
