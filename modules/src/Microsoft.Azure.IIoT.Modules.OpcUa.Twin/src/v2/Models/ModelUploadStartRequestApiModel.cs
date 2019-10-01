// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Upload start request model
    /// </summary>
    public class ModelUploadStartRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ModelUploadStartRequestApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ModelUploadStartRequestApiModel(ModelUploadStartRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            ContentEncoding = model.ContentEncoding;
            Diagnostics = model.Diagnostics == null ? null :
                new DiagnosticsApiModel(model.Diagnostics);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ModelUploadStartRequestModel ToServiceModel() {
            return new ModelUploadStartRequestModel {
                ContentEncoding = ContentEncoding,
                Diagnostics = Diagnostics?.ToServiceModel()
            };
        }

        /// <summary>
        /// Desired content encoding
        /// </summary>
        [JsonProperty(PropertyName = "ContentEncoding",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContentEncoding { get; set; }

        /// <summary>
        /// Optional diagnostics configuration
        /// </summary>
        [JsonProperty(PropertyName = "Diagnostics",
            NullValueHandling = NullValueHandling.Ignore)]
        public DiagnosticsApiModel Diagnostics { get; set; }
    }
}
