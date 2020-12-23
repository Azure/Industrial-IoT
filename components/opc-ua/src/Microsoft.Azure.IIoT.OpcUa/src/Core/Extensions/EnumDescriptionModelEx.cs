// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Enum Description extensions
    /// </summary>
    public static class EnumDescriptionModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EnumDescriptionModel Clone(this EnumDescriptionModel model) {
            if (model == null) {
                return null;
            }
            return new EnumDescriptionModel {
                BuiltInType = model.BuiltInType,
                Name = model.Name,
                DataTypeId = model.DataTypeId,
                EnumDefinition = model.EnumDefinition.Clone()
            };
        }
    }
}