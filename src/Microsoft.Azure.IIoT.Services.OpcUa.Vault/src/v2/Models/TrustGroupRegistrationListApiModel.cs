// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;

    /// <summary>
    /// Trust group registration collection model
    /// </summary>
    public sealed class TrustGroupRegistrationListApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public TrustGroupRegistrationListApiModel() {
        }

        /// <summary>
        /// Create model
        /// </summary>
        /// <param name="model"></param>
        public TrustGroupRegistrationListApiModel(
            TrustGroupRegistrationListModel model) {
            Registrations = model.Registrations
                .Select(g => new TrustGroupRegistrationApiModel(g))
                .ToList();
            NextPageLink = model.NextPageLink;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public TrustGroupRegistrationListModel ToServiceModel() {
            return new TrustGroupRegistrationListModel {
                Registrations = Registrations?
.Select(g => g.ToServiceModel())
.ToList(),
                NextPageLink = NextPageLink,
            };
        }

        /// <summary>
        /// Group registrations
        /// </summary>
        [JsonProperty(PropertyName = "registrations")]
        [DefaultValue(null)]
        public List<TrustGroupRegistrationApiModel> Registrations { get; set; }

        /// <summary>
        /// Next link
        /// </summary>
        [JsonProperty(PropertyName = "nextPageLink",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string NextPageLink { get; set; }
    }
}
