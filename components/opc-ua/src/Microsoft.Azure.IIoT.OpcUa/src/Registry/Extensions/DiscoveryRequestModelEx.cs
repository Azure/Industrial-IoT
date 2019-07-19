// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Models {

    /// <summary>
    /// Discovery request model extensions
    /// </summary>
    public static class DiscoveryRequestModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DiscoveryRequestModel Clone(this DiscoveryRequestModel model) {
            if (model == null) {
                return null;
            }
            return new DiscoveryRequestModel {
                Configuration = model.Configuration.Clone(),
                Discovery = model.Discovery,
                Context = model.Context?.Clone(),
                Id = model.Id
            };
        }
    }
}
