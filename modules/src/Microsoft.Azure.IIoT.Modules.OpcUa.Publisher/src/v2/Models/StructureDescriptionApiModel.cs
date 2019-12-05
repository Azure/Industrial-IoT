// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Modules.OpcUa.Publisher.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Twin.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// A Structure description
    /// </summary>
    public class StructureDescriptionApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public StructureDescriptionApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public StructureDescriptionApiModel(StructureDescriptionModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            StructureDefinition = model.StructureDefinition == null ? null :
                new StructureDefinitionApiModel(model.StructureDefinition);
            Name = model.Name;
            DataTypeId = model.DataTypeId;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public StructureDescriptionModel ToServiceModel() {
            return new StructureDescriptionModel {
                DataTypeId = DataTypeId,
                Name = Name,
                StructureDefinition = StructureDefinition?.ToServiceModel()
            };
        }

        /// <summary>
        /// Data type id
        /// </summary>
        [JsonProperty(PropertyName = "dataTypeId")]
        public string DataTypeId { get; set; }

        /// <summary>
        /// The qualified name of the data type.
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Structure definition
        /// </summary>
        [JsonProperty(PropertyName = "structureDefinition")]
        public StructureDefinitionApiModel StructureDefinition { get; set; }
    }
}
