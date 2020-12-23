// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Structure Description extensions
    /// </summary>
    public static class StructureDescriptionModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static StructureDescriptionModel Clone(this StructureDescriptionModel model) {
            if (model == null) {
                return null;
            }
            return new StructureDescriptionModel {
                DataTypeId = model.DataTypeId,
                Name = model.Name,
                StructureDefinition = model.StructureDefinition.Clone()
            };
        }
    }
}