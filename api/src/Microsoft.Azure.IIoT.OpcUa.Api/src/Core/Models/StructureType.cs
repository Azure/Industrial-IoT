// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Api.Core.Models {
    using System.Runtime.Serialization;

    /// <summary>
    /// Type of structure
    /// </summary>
    [DataContract]
    public enum StructureType {

        /// <summary>
        /// Default structure
        /// </summary>
        [EnumMember]
        Structure = 0,

        /// <summary>
        /// Structure has optional fields
        /// </summary>
        [EnumMember]
        StructureWithOptionalFields = 1,

        /// <summary>
        /// Union
        /// </summary>
        [EnumMember]
        Union = 2
    }
}
