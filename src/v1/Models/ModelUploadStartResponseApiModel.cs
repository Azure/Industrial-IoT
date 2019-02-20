// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Model upload start response
    /// </summary>
    public class ModelUploadStartResponseApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public ModelUploadStartResponseApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public ModelUploadStartResponseApiModel(ModelUploadStartResultModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            BlobName = model.BlobName;
            ContentEncoding = model.ContentEncoding;
            TimeStamp = model.TimeStamp;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public ModelUploadStartResultModel ToServiceModel() {
            return new ModelUploadStartResultModel {
                BlobName = BlobName,
                ContentEncoding = ContentEncoding,
                TimeStamp = TimeStamp
            };
        }

        /// <summary>
        /// Blob File name
        /// </summary>
        [JsonProperty(PropertyName = "BlobName",
            NullValueHandling = NullValueHandling.Ignore)]
        public string BlobName { get; set; }

        /// <summary>
        /// Content encoding
        /// </summary>
        [JsonProperty(PropertyName = "ContentEncoding",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ContentEncoding { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        [JsonProperty(PropertyName = "TimeStamp",
            NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? TimeStamp { get; set; }
    }
}
