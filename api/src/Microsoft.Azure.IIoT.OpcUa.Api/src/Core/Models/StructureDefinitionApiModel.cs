// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;
    using System.Collections.Generic;

    /// <summary>
    /// Structure definition
    /// </summary>
    [DataContract]
    public class StructureDefinitionApiModel {

        /// <summary>
        /// Base data type of the structure
        /// </summary>
        [DataMember(Name = "baseDataTypeId",
            EmitDefaultValue = false)]
        public string BaseDataTypeId { get; set; }

        /// <summary>
        /// Type of structure
        /// </summary>
        [DataMember(Name = "structureType")]
        public StructureType StructureType { get; set; }

        /// <summary>
        /// Fields in the structure or union
        /// </summary>
        [DataMember(Name = "fields")]
        public List<StructureFieldApiModel> Fields { get; set; }
    }
}
