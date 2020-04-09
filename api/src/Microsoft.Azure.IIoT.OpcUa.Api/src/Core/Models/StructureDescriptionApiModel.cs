// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// A Structure description
    /// </summary>
    [DataContract]
    public class StructureDescriptionApiModel {

        /// <summary>
        /// Data type id
        /// </summary>
        [DataMember(Name = "dataTypeId", Order = 0)]
        public string DataTypeId { get; set; }

        /// <summary>
        /// The qualified name of the data type.
        /// </summary>
        [DataMember(Name = "name", Order = 1)]
        public string Name { get; set; }

        /// <summary>
        /// Structure definition
        /// </summary>
        [DataMember(Name = "structureDefinition", Order = 2)]
        public StructureDefinitionApiModel StructureDefinition { get; set; }
    }
}
