// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.IIoT.OpcUa.Models {

    /// <summary>
    /// Arguments to pass
    /// </summary>
    public class MethodArgumentModel {

        /// <summary>
        /// Name of the argument
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of argument
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Initial value or value to use
        /// </summary>
        public JToken Value { get; set; }

        /// <summary>
        /// Human readable name of the type to provide
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Opc Data type id of the value
        /// </summary>
        public string TypeId { get; set; }

        /// <summary>
        /// Optional, scalar if not set
        /// </summary>
        public int? ValueRank { get; set; }

        /// <summary>
        /// Optional Array dimension of argument
        /// </summary>
        public uint[] ArrayDimensions { get; set; }
    }
}
