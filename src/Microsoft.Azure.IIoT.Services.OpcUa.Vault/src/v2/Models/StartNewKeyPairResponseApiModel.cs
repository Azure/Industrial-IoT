// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Vault.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Vault.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// New key pair response
    /// </summary>
    public sealed class StartNewKeyPairRequestResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public StartNewKeyPairRequestResponseApiModel() {
        }

        /// <summary>
        /// Create new response
        /// </summary>
        /// <param name="model"></param>
        public StartNewKeyPairRequestResponseApiModel(StartNewKeyPairRequestResultModel model) {
            RequestId = model.RequestId;
        }

        /// <summary>
        /// Convert to service model
        /// </summary>
        public StartNewKeyPairRequestResultModel ToServiceModel() {
            return new StartNewKeyPairRequestResultModel {
                RequestId = RequestId,
            };
        }

        /// <summary>
        /// Request id
        /// </summary>
        [JsonProperty(PropertyName = "requestId")]
        [Required]
        public string RequestId { get; set; }
    }
}
