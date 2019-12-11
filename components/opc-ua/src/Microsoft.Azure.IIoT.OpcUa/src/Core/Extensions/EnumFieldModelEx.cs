// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    /// <summary>
    /// Enum field extensions
    /// </summary>
    public static class EnumFieldModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EnumFieldModel Clone(this EnumFieldModel model) {
            if (model == null) {
                return null;
            }
            return new EnumFieldModel {
                Value = model.Value,
                Name = model.Name,
                Description = model.Description.Clone(),
                DisplayName = model.DisplayName
            };
        }
    }
}