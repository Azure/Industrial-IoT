// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Linq;
    using System.ComponentModel;
    using System;

    /// <summary>
    /// Call request model
    /// </summary>
    /// <remarks>
    /// A method call request can specify the targets in several ways:
    /// <ul>
    /// <li>Specify methodId and objectId node ids and leave the browse
    /// paths null.</li>
    /// <li>Specify an objectBrowsePath to a real object node from
    /// the node specified with objectId.  If objectId is null, the
    /// root node (i=84) is used. </li>
    /// <li>Specify a methodBrowsePath from the above object node
    /// to the actual method node to call on the object.  methodId
    /// remains null.</li>
    /// <li>Like previously, but specify methodId and method browse
    /// path from it to a real method node.</li>
    /// </ul>
    /// </remarks>
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
        [JsonProperty(PropertyName = "methodId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string MethodId { get; set; }

        /// <summary>
        /// Context of the method, i.e. an object or object type
        /// node.
        /// </summary>
        [JsonProperty(PropertyName = "objectId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string ObjectId { get; set; }

        /// <summary>
        /// Arguments for the method - null means no args
        /// </summary>
        [JsonProperty(PropertyName = "arguments",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<MethodCallArgumentApiModel> Arguments { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// MethodId or from a resolved objectId to the actual
        /// method node.
        /// </summary>
        [JsonProperty(PropertyName = "methodBrowsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string[] MethodBrowsePath { get; set; }

        /// <summary>
        /// An optional component path from the node identified by
        /// ObjectId to the actual object or objectType node.
        /// If ObjectId is null, the root node (i=84) is used.
        /// </summary>
        [JsonProperty(PropertyName = "objectBrowsePath",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public string[] ObjectBrowsePath { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
