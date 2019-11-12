// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Pub sub job model with defaults
    /// </summary>
    public class PubSubJobApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PubSubJobApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PubSubJobApiModel(PubSubJobModel model) {
            if (model?.Job == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Job = new DataSetWriterGroupApiModel(model.Job);
            ConnectionString = model.ConnectionString;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PubSubJobModel ToServiceModel() {
            return new PubSubJobModel {
                Job = Job?.ToServiceModel(),
                ConnectionString = ConnectionString
            };
        }

        /// <summary>
        /// Pub sub job
        /// </summary>
        [JsonProperty(PropertyName = "job")]
        public DataSetWriterGroupApiModel Job { get; set; }

        /// <summary>
        /// Connection string
        /// </summary>
        [JsonProperty(PropertyName = "connectionString")]
        public string ConnectionString { get; set; }
    }
}