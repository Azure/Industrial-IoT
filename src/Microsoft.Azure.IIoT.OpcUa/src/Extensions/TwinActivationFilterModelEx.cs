// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Models {
    using System.Linq;

    /// <summary>
    /// Twin activation filter model extensions
    /// </summary>
    public static class TwinActivationFilterModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static TwinActivationFilterModel Clone(this TwinActivationFilterModel model) {
            if (model == null) {
                return null;
            }
            return new TwinActivationFilterModel {
                SecurityMode = model.SecurityMode,
                SecurityPolicies = model.SecurityPolicies?.ToList(),
                TrustLists = model.TrustLists?.ToList()
            };
        }
    }
}
