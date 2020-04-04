// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Publisher.Models {
    using Microsoft.Azure.IIoT.OpcUa.Api.Core.Models;
    using System.Runtime.Serialization;
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Metadata for the published dataset
    /// </summary>
    [DataContract]
    public class DataSetMetaDataApiModel {

        /// <summary>
        /// Name of the dataset
        /// </summary>
        [DataMember(Name = "name", Order = 0,
            EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Description of the dataset
        /// </summary>
        [DataMember(Name = "description", Order = 1,
            EmitDefaultValue = false)]
        public LocalizedTextApiModel Description { get; set; }

        /// <summary>
        /// Metadata for the data set fiels
        /// </summary>
        [DataMember(Name = "fields", Order = 2,
            EmitDefaultValue = false)]
        public List<FieldMetaDataApiModel> Fields { get; set; }

        /// <summary>
        /// Dataset class id
        /// </summary>
        [DataMember(Name = "dataSetClassId", Order = 3,
            EmitDefaultValue = false)]
        public Guid DataSetClassId { get; set; }

        /// <summary>
        /// Dataset version
        /// </summary>
        [DataMember(Name = "configurationVersion", Order = 4,
            EmitDefaultValue = false)]
        public ConfigurationVersionApiModel ConfigurationVersion { get; set; }

        /// <summary>
        /// Namespaces in the metadata description
        /// </summary>
        [DataMember(Name = "namespaces", Order = 5,
            EmitDefaultValue = false)]
        public List<string> Namespaces { get; set; }

        /// <summary>
        /// Structure data types
        /// </summary>
        [DataMember(Name = "structureDataTypes", Order = 6,
            EmitDefaultValue = false)]
        public List<StructureDescriptionApiModel> StructureDataTypes { get; set; }

        /// <summary>
        /// Enum data types
        /// </summary>
        [DataMember(Name = "enumDataTypes", Order = 7,
            EmitDefaultValue = false)]
        public List<EnumDescriptionApiModel> EnumDataTypes { get; set; }

        /// <summary>
        /// Simple data type.
        /// </summary>
        [DataMember(Name = "simpleDataTypes", Order = 8,
            EmitDefaultValue = false)]
        public List<SimpleTypeDescriptionApiModel> SimpleDataTypes { get; set; }
    }
}
