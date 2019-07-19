// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;

    /// <summary>
    /// Finish request results
    /// </summary>
    public sealed class FinishNewKeyPairRequestResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public FinishNewKeyPairRequestResponseApiModel() {
        }

        /// <summary>
        /// Create fetch request
        /// </summary>
        /// <param name="model"></param>
        public FinishNewKeyPairRequestResponseApiModel(FinishNewKeyPairRequestResultModel model) {
            Request = model.Request != null ?
                new CertificateRequestRecordApiModel(model.Request) : null;
            Certificate = model.Certificate != null ?
                new X509CertificateApiModel(model.Certificate) : null;
            PrivateKey = model != null ?
                new PrivateKeyApiModel(model.PrivateKey) : null;
        }

        /// <summary>
        /// Create fetch request
        /// </summary>
        public FinishNewKeyPairRequestResultModel ToServiceModel() {
            return new FinishNewKeyPairRequestResultModel {
                Request = Request?.ToServiceModel(),
                Certificate = Certificate.ToServiceModel(),
                PrivateKey = PrivateKey?.ToServiceModel()
            };
        }

        /// <summary>
        /// Request
        /// </summary>
        [JsonProperty(PropertyName = "request",
            NullValueHandling = NullValueHandling.Ignore)]
        public CertificateRequestRecordApiModel Request { get; set; }

        /// <summary>
        /// Signed certificate
        /// </summary>
        [JsonProperty(PropertyName = "certificate",
            NullValueHandling = NullValueHandling.Ignore)]
        public X509CertificateApiModel Certificate { get; set; }

        /// <summary>
        /// Private key
        /// </summary>
        [JsonProperty(PropertyName = "privateKey",
            NullValueHandling = NullValueHandling.Ignore)]
        public PrivateKeyApiModel PrivateKey { get; set; }
    }
}
