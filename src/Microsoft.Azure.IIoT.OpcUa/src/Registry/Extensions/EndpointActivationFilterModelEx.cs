// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {
    using System.Linq;

    /// <summary>
    /// Endpoint Activation Filter model extensions
    /// </summary>
    public static class EndpointActivationFilterModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static EndpointActivationFilterModel Clone(this EndpointActivationFilterModel model) {
            if (model == null) {
                return null;
            }
            return new EndpointActivationFilterModel {
                SecurityMode = model.SecurityMode,
                SecurityPolicies = model.SecurityPolicies?.ToList(),
                TrustLists = model.TrustLists?.ToList()
            };
        }
    }
}
