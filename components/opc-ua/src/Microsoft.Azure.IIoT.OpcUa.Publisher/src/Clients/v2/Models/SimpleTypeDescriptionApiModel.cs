// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Clients.v2.Models {
    using Microsoft.Azure.IIoT.OpcUa.Core.Models;
    using Newtonsoft.Json;
    using System;

    /// <summary>
    /// Simple type
    /// </summary>
    public class SimpleTypeDescriptionApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public SimpleTypeDescriptionApiModel() { }

        /// <summary>
        /// Create api model from service model
        /// </summary>
        /// <param name="model"></param>
        public SimpleTypeDescriptionApiModel(SimpleTypeDescriptionModel model) {
            if (model == null) {
                throw new ArgumentNullException(nameof(model));
            }
            BaseDataTypeId = model.BaseDataTypeId;
            Name = model.Name;
            DataTypeId = model.DataTypeId;
            BuiltInType = model.BuiltInType;
        }

        /// <summary>
        /// Create service model from api model
        /// </summary>
        public SimpleTypeDescriptionModel ToServiceModel() {
            return new SimpleTypeDescriptionModel {
                BaseDataTypeId = BaseDataTypeId,
                Name = Name,
                DataTypeId = DataTypeId,
                BuiltInType = BuiltInType
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
        /// Base data type of the type
        /// </summary>
        [JsonProperty(PropertyName = "baseDataTypeId",
            NullValueHandling = NullValueHandling.Ignore)]
        public string BaseDataTypeId { get; set; }

        /// <summary>
        /// The built in type
        /// </summary>
        [JsonProperty(PropertyName = "builtInType",
            NullValueHandling = NullValueHandling.Ignore)]
        public string BuiltInType { get; set; }
    }
}
