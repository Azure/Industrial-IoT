// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Modules.Twin.v1.Models {
    using Microsoft.Azure.IIoT.OpcUa.Models;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// method arg model for twin module
    /// </summary>
    public class MethodArgumentApiModel {

        /// <summary>
        /// Default constructor
        /// </summary>
        public MethodArgumentApiModel() {}

        /// <summary>
        /// Create from service model
        /// </summary>
        /// <param name="model"></param>
        public MethodArgumentApiModel(MethodArgumentModel model) {
            Value = model.Value;
            TypeId = model.TypeId;
            ValueRank = model.ValueRank;
            Name = model.Name;
            TypeName = model.TypeName;
            Description = model.Description;
            ArrayDimensions = model.ArrayDimensions;
        }

        /// <summary>
        /// Convert back to service model
        /// </summary>
        /// <returns></returns>
        public MethodArgumentModel ToServiceModel() {
            return new MethodArgumentModel {
                Value = Value,
                TypeId = TypeId,
                ValueRank = ValueRank,
                ArrayDimensions = ArrayDimensions,
                Description = Description,
                Name = Name,
                TypeName = TypeName
            };
        }

        /// <summary>
        /// Initial value or value to use
        /// </summary>
        public JToken Value { get; set; }

        /// <summary>
        /// Data type of the value
        /// </summary>
        public string TypeId { get; set; }

        /// <summary>
        /// Optional, scalar if not set
        /// </summary>
        public int? ValueRank { get; set; }

        /// <summary>
        /// Optional, array dimension
        /// </summary>
        public uint[] ArrayDimensions { get; set; }

        /// <summary>
        /// Optional, argument name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Optional, type name
        /// </summary>
        public string TypeName { get; set; }

        /// <summary>
        /// Optional, description
        /// </summary>
        public string Description { get; set; }
    }
}
