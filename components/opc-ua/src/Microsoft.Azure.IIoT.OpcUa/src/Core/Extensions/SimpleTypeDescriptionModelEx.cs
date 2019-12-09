// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Simple type description extensions
    /// </summary>
    public static class SimpleTypeDescriptionModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static SimpleTypeDescriptionModel Clone(this SimpleTypeDescriptionModel model) {
            if (model == null) {
                return null;
            }
            return new SimpleTypeDescriptionModel {
                Name = model.Name,
                DataTypeId = model.DataTypeId,
                BaseDataTypeId = model.BaseDataTypeId,
                BuiltInType = model.BuiltInType
            };
        }
    }
}