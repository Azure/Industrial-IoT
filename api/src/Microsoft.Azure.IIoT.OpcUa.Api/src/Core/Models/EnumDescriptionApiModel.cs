// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Describes the enumeration
    /// </summary>
    [DataContract]
    public class EnumDescriptionApiModel {

        /// <summary>
        /// Data type id
        /// </summary>
        [DataMember(Name = "dataTypeId")]
        public string DataTypeId { get; set; }

        /// <summary>
        /// The qualified name of the enum
        /// </summary>
        [DataMember(Name = "name",
            EmitDefaultValue = false)]
        public string Name { get; set; }

        /// <summary>
        /// Enum definition
        /// </summary>
        [DataMember(Name = "enumDefinition")]
        public EnumDefinitionApiModel EnumDefinition { get; set; }

        /// <summary>
        /// The built in type of the enum
        /// </summary>
        [DataMember(Name = "builtInType",
            EmitDefaultValue = false)]
        public string BuiltInType { get; set; }
    }
}
