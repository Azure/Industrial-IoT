// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Twin.Models {
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of structure
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StructureType {

        /// <summary>
        /// Default structure
        /// </summary>
        Structure = 0,

        /// <summary>
        /// Structure has optional fields
        /// </summary>
        StructureWithOptionalFields = 1,

        /// <summary>
        /// Union
        /// </summary>
        Union = 2
    }
}
