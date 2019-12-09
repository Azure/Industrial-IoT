﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Core.Models {
    using System.Linq;

    /// <summary>
    /// Enum definition extensions
    /// </summary>
    public static class EnumDefinitionModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EnumDefinitionModel Clone(this EnumDefinitionModel model) {
            if (model == null) {
                return null;
            }
            return new EnumDefinitionModel {
                Fields = model.Fields?.Select(f => f.Clone()).ToList()
            };
        }
    }
}