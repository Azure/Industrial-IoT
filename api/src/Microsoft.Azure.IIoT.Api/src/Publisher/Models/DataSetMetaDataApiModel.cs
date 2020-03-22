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
        [DataMember(Name = "name",
            EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Description of the dataset
        /// </summary>
        [DataMember(Name = "description",
            EmitDefaultValue = false)]
        public LocalizedTextApiModel Description { get; set; }

        /// <summary>
        /// Metadata for the data set fiels
        /// </summary>
        [DataMember(Name = "fields",
            EmitDefaultValue = false)]
        public List<FieldMetaDataApiModel> Fields { get; set; }

        /// <summary>
        /// Dataset class id
        /// </summary>
        [DataMember(Name = "dataSetClassId",
            EmitDefaultValue = false)]
        public Guid DataSetClassId { get; set; }

        /// <summary>
        /// Dataset version
        /// </summary>
        [DataMember(Name = "configurationVersion",
            EmitDefaultValue = false)]
        public ConfigurationVersionApiModel ConfigurationVersion { get; set; }

        /// <summary>
        /// Namespaces in the metadata description
        /// </summary>
        [DataMember(Name = "namespaces",
            EmitDefaultValue = false)]
        public List<string> Namespaces { get; set; }

        /// <summary>
        /// Structure data types
        /// </summary>
        [DataMember(Name = "structureDataTypes",
            EmitDefaultValue = false)]
        public List<StructureDescriptionApiModel> StructureDataTypes { get; set; }

        /// <summary>
        /// Enum data types
        /// </summary>
        [DataMember(Name = "enumDataTypes",
            EmitDefaultValue = false)]
        public List<EnumDescriptionApiModel> EnumDataTypes { get; set; }

        /// <summary>
        /// Simple data type.
        /// </summary>
        [DataMember(Name = "simpleDataTypes",
            EmitDefaultValue = false)]
        public List<SimpleTypeDescriptionApiModel> SimpleDataTypes { get; set; }
    }
}
