// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Method metadata request model for module
    /// </summary>
    public class MethodMetadataRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodMetadataRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodMetadataRequestApiModel(MethodMetadataRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            MethodId = model.MethodId;
            MethodBrowsePath = model.MethodBrowsePath;
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodMetadataRequestModel ToServiceModel() {
            return new MethodMetadataRequestModel {
                MethodId = MethodId,
                MethodBrowsePath = MethodBrowsePath,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Method id of method to call.
        /// </summary>
        [JsonProperty(PropertyName = "MethodId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string MethodId { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// MethodId to the actual method node.
        /// </summary>
        [JsonProperty(PropertyName = "MethodBrowsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] MethodBrowsePath { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "Header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
