// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Simple type
    /// </summary>
    [DataContract]
    public class SimpleTypeDescriptionApiModel {

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
        /// Base data type of the type
        /// </summary>
        [DataMember(Name = "baseDataTypeId", Order = 2,
            EmitDefaultValue = false)]
        public string BaseDataTypeId { get; set; }

        /// <summary>
        /// The built in type
        /// </summary>
        [DataMember(Name = "builtInType", Order = 3,
            EmitDefaultValue = false)]
        public string BuiltInType { get; set; }
    }
}
