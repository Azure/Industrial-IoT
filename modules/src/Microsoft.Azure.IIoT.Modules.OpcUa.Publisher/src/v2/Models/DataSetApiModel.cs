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
    public class DataSetApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DataSetApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public DataSetApiModel(DataSetModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Name = model.Name;
            DataSetMajorVersion = model.DataSetMajorVersion;
            DataSetMinorVersion = model.DataSetMinorVersion;
            Fields = model.Fields?
                .Select(f => new DataSetFieldApiModel(f)).ToList();
            TypeId = model.TypeId;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DataSetModel ToServiceModel() {
            return new DataSetModel {
                Name = Name,
                DataSetMajorVersion = DataSetMajorVersion,
                DataSetMinorVersion = DataSetMinorVersion,
                Fields = Fields?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                TypeId = TypeId,
            };
        }

        /// <summary>
        /// Name of dataset
        /// </summary>
        [JsonProperty(PropertyName = "name",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Type id
        /// </summary>
        [JsonProperty(PropertyName = "typeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string TypeId { get; set; }

        /// <summary>
        /// Dataset major version
        /// </summary>
        [JsonProperty(PropertyName = "dataSetMajorVersion",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? DataSetMajorVersion { get; set; }

        /// <summary>
        /// Dataset minor version
        /// </summary>
        [JsonProperty(PropertyName = "dataSetMinorVersion",
            NullValueHandling = NullValueHandling.Ignore)]
        public uint? DataSetMinorVersion { get; set; }

        /// <summary>
        /// Fields of the dataset
        /// </summary>
        [JsonProperty(PropertyName = "fields")]
        public List<DataSetFieldApiModel> Fields { get; set; }
    }
}