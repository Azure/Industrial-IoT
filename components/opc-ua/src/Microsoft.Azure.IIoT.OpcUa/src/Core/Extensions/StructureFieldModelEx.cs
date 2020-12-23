// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Linq;

    /// <summary>
    /// Structure field extensions
    /// </summary>
    public static class StructureFieldModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static StructureFieldModel Clone(this StructureFieldModel model) {
            if (model == null) {
                return null;
            }
            return new StructureFieldModel {
                ValueRank = model.ValueRank,
                Name = model.Name,
                ArrayDimensions = model.ArrayDimensions?.ToList(),
                DataTypeId = model.DataTypeId,
                Description = model.Description.Clone(),
                IsOptional = model.IsOptional,
                MaxStringLength = model.MaxStringLength
            };
        }
    }
}