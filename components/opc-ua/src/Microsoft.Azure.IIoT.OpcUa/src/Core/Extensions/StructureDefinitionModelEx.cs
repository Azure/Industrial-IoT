// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Linq;

    /// <summary>
    /// Structure Definition extensions
    /// </summary>
    public static class StructureDefinitionModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static StructureDefinitionModel Clone(this StructureDefinitionModel model) {
            if (model == null) {
                return null;
            }
            return new StructureDefinitionModel {
                BaseDataTypeId = model.BaseDataTypeId,
                Fields = model.Fields?.Select(f => f.Clone()).ToList(),
                StructureType = model.StructureType
            };
        }
    }
}