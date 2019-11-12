// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Dataset Field model
    /// </summary>
    public class DataSetFieldApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DataSetFieldApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public DataSetFieldApiModel(DataSetFieldModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            NodeId = model.NodeId;
            Configuration = model.Configuration == null ? null :
                new DataSetFieldSamplingApiModel(model.Configuration);

        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DataSetFieldModel ToServiceModel() {
            return new DataSetFieldModel {
                NodeId = NodeId,
                Configuration = Configuration?.ToServiceModel()
            };
        }

        /// <summary>
        /// Node id
        /// </summary>
        [JsonProperty(PropertyName = "nodeId")]
        public string NodeId { get; set; }

        /// <summary>
        /// Sampling configuration
        /// </summary>
        [JsonProperty(PropertyName = "configuration")]
        public DataSetFieldSamplingApiModel Configuration { get; set; }
    }
}