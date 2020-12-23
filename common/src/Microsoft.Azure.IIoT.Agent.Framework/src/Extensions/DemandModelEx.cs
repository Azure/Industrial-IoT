// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    /// <summary>
    /// Demand model extensions
    /// </summary>
    public static class DemandModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DemandModel Clone(this DemandModel model) {
            if (model == null) {
                return null;
            }
            return new DemandModel {
                Key = model.Key,
                Value = model.Value,
                Operator = model.Operator
            };
        }
    }
}