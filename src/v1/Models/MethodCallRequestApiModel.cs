// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Call request model for module
    /// </summary>
    public class MethodCallRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodCallRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodCallRequestApiModel(MethodCallRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            MethodId = model.MethodId;
            ObjectId = model.ObjectId;
            MethodBrowsePath = model.MethodBrowsePath;
            ObjectBrowsePath = model.ObjectBrowsePath;
            Arguments = model.Arguments?
                .Select(a => a != null ? new MethodCallArgumentApiModel(a) : null)
                .ToList();
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodCallRequestModel ToServiceModel() {
            return new MethodCallRequestModel {
                MethodId = MethodId,
                ObjectId = ObjectId,
                MethodBrowsePath = MethodBrowsePath,
                ObjectBrowsePath = ObjectBrowsePath,
                Arguments = Arguments?
                    .Select(s => s?.ToServiceModel()).ToList(),
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
        /// Context of the method, i.e. an object or object type
        /// node.
        /// </summary>
        [JsonProperty(PropertyName = "ObjectId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Arguments for the method - null means no args
        /// </summary>
        [JsonProperty(PropertyName = "Arguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodCallArgumentApiModel> Arguments { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// MethodId or from a resolved objectId to the actual
        /// method node.
        /// </summary>
        [JsonProperty(PropertyName = "MethodBrowsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] MethodBrowsePath { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// ObjectId to the actual object or objectType node.
        /// If ObjectId is null, the root node (i=84) is used.
        /// </summary>
        [JsonProperty(PropertyName = "ObjectBrowsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        public string[] ObjectBrowsePath { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "Header",
            NullValueHandling = NullValueHandling.Ignore)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
