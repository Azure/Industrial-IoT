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
    /// Crl collection model
    /// </summary>
    public sealed class X509CrlChainApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public X509CrlChainApiModel() {
        }

        /// <summary>
        /// Create api model
        /// </summary>
        /// <param name="model"></param>
        public X509CrlChainApiModel(X509CrlChainModel model) {
            Chain = model?.Chain?
                .Select(c => new X509CrlApiModel(c))
                .ToList();
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public X509CrlChainModel ToServiceModel() {
            return new X509CrlChainModel {
                Chain = Chain?
                    .Select(c => c.ToServiceModel())
                    .ToList()
            };
        }

        /// <summary>
        /// Chain
        /// </summary>
        [JsonProperty(PropertyName = "chain",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<X509CrlApiModel> Chain { get; set; }
    }
}
