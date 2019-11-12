// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Pub/sub job description
    /// </summary>
    public class DataSetWriterGroupApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DataSetWriterGroupApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public DataSetWriterGroupApiModel(DataSetWriterGroupModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            DataSetWriter = model.DataSetWriter == null ? null :
                new DataSetWriterApiModel(model.DataSetWriter);
            Connection = model.Connection == null ? null :
                new ConnectionApiModel(model.Connection);
            Engine = model.Engine == null ? null :
                new EngineConfigurationApiModel(model.Engine);
            PublishingInterval = model.PublishingInterval;
            SendChangeMessages = model.SendChangeMessages;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DataSetWriterGroupModel ToServiceModel() {
            return new DataSetWriterGroupModel {
                DataSetWriter = DataSetWriter?.ToServiceModel(),
                Connection = Connection?.ToServiceModel(),
                Engine = Engine?.ToServiceModel(),
                PublishingInterval = PublishingInterval,
                SendChangeMessages = SendChangeMessages
            };
        }

        /// <summary>
        /// Dataset writer configuration
        /// </summary>
        [JsonProperty(PropertyName = "dataSetWriter")]
        public DataSetWriterApiModel DataSetWriter { get; set; }

        /// <summary>
        /// Connection configuration
        /// </summary>
        [JsonProperty(PropertyName = "connection",
            NullValueHandling = NullValueHandling.Ignore)]
        public ConnectionApiModel Connection { get; set; }

        /// <summary>
        /// Publish interval
        /// </summary>
        [JsonProperty(PropertyName = "publishingInterval",
            NullValueHandling = NullValueHandling.Ignore)]
        public TimeSpan? PublishingInterval { get; set; }

        /// <summary>
        /// Send change messages
        /// </summary>
        [JsonProperty(PropertyName = "sendChangeMessages",
            NullValueHandling = NullValueHandling.Ignore)]
        public bool? SendChangeMessages { get; set; }

        /// <summary>
        /// Engine configuration
        /// </summary>
        [JsonProperty(PropertyName = "configuration",
            NullValueHandling = NullValueHandling.Ignore)]
        public EngineConfigurationApiModel Engine { get; set; }
    }
}