// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Dataset model
    /// </summary>
    public class PublishedDataSetApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public PublishedDataSetApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public PublishedDataSetApiModel(PublishedDataSetModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Name = model.Name;
            DataSetSource = model.DataSetSource == null ? null :
                new PublishedDataSetSourceApiModel(model.DataSetSource);
            DataSetMetaData = model.DataSetMetaData == null ? null :
                new DataSetMetaDataApiModel(model.DataSetMetaData);
            ExtensionFields = model.ExtensionFields?
                .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public PublishedDataSetModel ToServiceModel() {
            return new PublishedDataSetModel {
                Name = Name,
                DataSetSource = DataSetSource?.ToServiceModel(),
                DataSetMetaData = DataSetMetaData?.ToServiceModel(),
                ExtensionFields = ExtensionFields?
                    .ToDictionary(k => k.Key, v => v.Value)
            };
        }

        /// <summary>
        /// Name of dataset
        /// </summary>
        [JsonProperty(PropertyName = "name",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Data set source
        /// </summary>
        [JsonProperty(PropertyName = "dataSetSource")]
        public PublishedDataSetSourceApiModel DataSetSource { get; set; }

        /// <summary>
        /// Dataset meta data to emit
        /// </summary>
        [JsonProperty(PropertyName = "dataSetMetaData",
            NullValueHandling = NullValueHandling.Ignore)]
        public DataSetMetaDataApiModel DataSetMetaData { get; set; }

        /// <summary>
        /// Extension fields
        /// </summary>
        [JsonProperty(PropertyName = "extensionFields",
            NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, string> ExtensionFields { get; set; }
    }
}