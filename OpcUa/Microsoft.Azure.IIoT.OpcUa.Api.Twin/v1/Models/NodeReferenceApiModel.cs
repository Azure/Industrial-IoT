// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Services.Models;
    using Newtonsoft.Json;
    using System.ComponentModel.DataAnnotations;

    /// <summary>
    /// reference model for webservice api
    /// </summary>
    public class NodeReferenceApiModel {
        /// <summary>
        /// Default constructor
        /// </summary>
        public NodeReferenceApiModel() {}

        /// <summary>
        /// Create reference api model
        /// </summary>
        /// <param name="model"></param>
        public NodeReferenceApiModel(NodeReferenceModel model) {
            Id = model.Id;
            BrowseName = model.BrowseName;
            Text = model.Text;
            Target = new NodeApiModel(model.Target);
        }

        /// <summary>
        /// Reference Type id
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        [Required]
        public string Id { get; set; }

        /// <summary>
        /// Browse name of reference
        /// </summary>
        [JsonProperty(PropertyName = "browseName")]
        [Required]
        public string BrowseName { get; set; }

        /// <summary>
        /// Target node
        /// </summary>
        [JsonProperty(PropertyName = "target")]
        [Required]
        public NodeApiModel Target { get; set; }

        /// <summary>
        /// Display name of reference
        /// </summary>
        [JsonProperty(PropertyName = "text",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
    }
}
