// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.Discovery.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Registry operation log model
    /// </summary>
    public class RegistryOperationContextApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public RegistryOperationContextApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public RegistryOperationContextApiModel(RegistryOperationContextModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Time = model.Time;
            AuthorityId = model.AuthorityId;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public RegistryOperationContextModel ToServiceModel() {
            return new RegistryOperationContextModel {
                AuthorityId = AuthorityId,
                Time = Time
            };
        }

        /// <summary>
        /// User
        /// </summary>
        [JsonProperty(PropertyName = "AuthorityId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string AuthorityId { get; set; }

        /// <summary>
        /// Operation time
        /// </summary>
        [JsonProperty(PropertyName = "Time",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime Time { get; set; }
    }
}

