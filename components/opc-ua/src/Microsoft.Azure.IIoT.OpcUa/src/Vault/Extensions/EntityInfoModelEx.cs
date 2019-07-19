// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.OpcUa.Vault.Models {
    using System.Linq;

    /// <summary>
    /// Entity info model extensions
    /// </summary>
    public static class EntityInfoModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EntityInfoModel Clone(this EntityInfoModel model) {
            if (model == null) {
                return null;
            }
            return new EntityInfoModel {
                Addresses = model.Addresses?.ToList(),
                Id = model.Id,
                Name = model.Name,
                Type = model.Type
            };
        }
    }
}
