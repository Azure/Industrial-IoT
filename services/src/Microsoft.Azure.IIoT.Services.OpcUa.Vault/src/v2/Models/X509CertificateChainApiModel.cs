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
    /// Certificate chain
    /// </summary>
    public sealed class X509CertificateChainApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public X509CertificateChainApiModel() {
        }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="model"></param>
        public X509CertificateChainApiModel(X509CertificateChainModel model) {
            Chain = model?.Chain?
                .Select(c => new X509CertificateApiModel(c))
                .ToList();
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public X509CertificateChainModel ToServiceModel() {
            return new X509CertificateChainModel {
                Chain = Chain?.Select(c => c.ToServiceModel()).ToList()
            };
        }

        /// <summary>
        /// Chain
        /// </summary>
        [JsonProperty(PropertyName = "chain",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<X509CertificateApiModel> Chain { get; set; }
    }
}
