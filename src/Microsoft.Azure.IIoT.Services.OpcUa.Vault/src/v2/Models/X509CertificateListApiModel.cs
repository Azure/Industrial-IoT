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
    /// Certificate list
    /// </summary>
    public sealed class X509CertificateListApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public X509CertificateListApiModel() {
        }

        /// <summary>
        /// Create collection
        /// </summary>
        /// <param name="model"></param>
        public X509CertificateListApiModel(X509CertificateListModel model) {
            NextPageLink = model.NextPageLink;
            Certificates = model?.Certificates?
                .Select(c => new X509CertificateApiModel(c))
                .ToList();
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        /// <returns></returns>
        public X509CertificateListModel ToServiceModel() {
            return new X509CertificateListModel {
                Certificates = Certificates?
                    .Select(c => c.ToServiceModel()).ToList(),
                NextPageLink = NextPageLink
            };
        }

        /// <summary>
        /// Certificates
        /// </summary>
        [JsonProperty(PropertyName = "certificates",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public List<X509CertificateApiModel> Certificates { get; set; }

        /// <summary>
        /// Next link
        /// </summary>
        [JsonProperty(PropertyName = "nextPageLink",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string NextPageLink { get; set; }
    }
}
