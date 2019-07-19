// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Services.OpcUa.Twin.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// Browse nodes by path
    /// </summary>
    public class BrowsePathRequestApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public BrowsePathRequestApiModel() { }

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public BrowsePathRequestApiModel(BrowsePathRequestModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NodeId = model.NodeId;
            BrowsePaths = model.BrowsePaths;
            ReadVariableValues = model.ReadVariableValues;
            Header = model.Header == null ? null :
                new RequestHeaderApiModel(model.Header);
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public BrowsePathRequestModel ToServiceModel() {
            return new BrowsePathRequestModel {
                NodeId = NodeId,
                BrowsePaths = BrowsePaths,
                ReadVariableValues = ReadVariableValues,
                Header = Header?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node to browse from.
        /// (default: RootFolder).
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// The paths to browse from node.
        /// (mandatory)
        /// </summary>
        [JsonProperty(PropertyName = "browsePaths")]
        [Required]
        public List<string[]> BrowsePaths { get; set; }

        /// <summary>
        /// Whether to read variable values on target nodes.
        /// (default is false)
        /// </summary>
        [JsonProperty(PropertyName = "readVariableValues",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(false)]
        public bool? ReadVariableValues { get; set; }

        /// <summary>
        /// Optional request header
        /// </summary>
        [JsonProperty(PropertyName = "header",
            NullValueHandling = NullValueHandling.Ignore)]
        [DefaultValue(null)]
        public RequestHeaderApiModel Header { get; set; }
    }
}
