// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Publisher.Models;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Metadata for the published dataset
    /// </summary>
    public class DataSetMetaDataApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public DataSetMetaDataApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public DataSetMetaDataApiModel(DataSetMetaDataModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            Name = model.Name;
            ConfigurationVersion = model.ConfigurationVersion == null ? null :
                new ConfigurationVersionApiModel(model.ConfigurationVersion);
            DataSetClassId = model.DataSetClassId;
            Description = model.Description == null ? null :
                new LocalizedTextApiModel(model.Description);
            Fields = model.Fields?
                .Select(f => new FieldMetaDataApiModel(f))
                .ToList();
            EnumDataTypes = model.EnumDataTypes?
                .Select(f => new EnumDescriptionApiModel(f))
                .ToList();
            StructureDataTypes = model.StructureDataTypes?
                .Select(f => new StructureDescriptionApiModel(f))
                .ToList();
            SimpleDataTypes = model.SimpleDataTypes?
                .Select(f => new SimpleTypeDescriptionApiModel(f))
                .ToList();
            Namespaces = model.Namespaces?
                .ToList();
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public DataSetMetaDataModel ToServiceModel() {
            return new DataSetMetaDataModel {
                Name = Name,
                ConfigurationVersion = ConfigurationVersion?.ToServiceModel(),
                DataSetClassId = DataSetClassId,
                Description = Description?.ToServiceModel(),
                Fields = Fields?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                EnumDataTypes = EnumDataTypes?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                StructureDataTypes = StructureDataTypes?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                SimpleDataTypes = SimpleDataTypes?
                    .Select(f => f.ToServiceModel())
                    .ToList(),
                Namespaces = Namespaces?.ToList()
            };
        }


        /// <summary>
        /// Name of the dataset
        /// </summary>
        [JsonProperty(PropertyName = "name",
            NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }

        /// <summary>
        /// Description of the dataset
        /// </summary>
        [JsonProperty(PropertyName = "description",
            NullValueHandling = NullValueHandling.Ignore)]
        public LocalizedTextApiModel Description { get; set; }

        /// <summary>
        /// Metadata for the data set fiels
        /// </summary>
        [JsonProperty(PropertyName = "fields",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<FieldMetaDataApiModel> Fields { get; set; }

        /// <summary>
        /// Dataset class id
        /// </summary>
        [JsonProperty(PropertyName = "dataSetClassId",
            NullValueHandling = NullValueHandling.Ignore)]
        public Guid DataSetClassId { get; set; }

        /// <summary>
        /// Dataset version
        /// </summary>
        [JsonProperty(PropertyName = "configurationVersion",
            NullValueHandling = NullValueHandling.Ignore)]
        public ConfigurationVersionApiModel ConfigurationVersion { get; set; }

        /// <summary>
        /// Namespaces in the metadata description
        /// </summary>
        [JsonProperty(PropertyName = "namespaces",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Namespaces { get; set; }

        /// <summary>
        /// Structure data types
        /// </summary>
        [JsonProperty(PropertyName = "structureDataTypes",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<StructureDescriptionApiModel> StructureDataTypes { get; set; }

        /// <summary>
        /// Enum data types
        /// </summary>
        [JsonProperty(PropertyName = "enumDataTypes",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<EnumDescriptionApiModel> EnumDataTypes { get; set; }

        /// <summary>
        /// Simple data type.
        /// </summary>
        [JsonProperty(PropertyName = "simpleDataTypes",
            NullValueHandling = NullValueHandling.Ignore)]
        public List<SimpleTypeDescriptionApiModel> SimpleDataTypes { get; set; }
    }
}
