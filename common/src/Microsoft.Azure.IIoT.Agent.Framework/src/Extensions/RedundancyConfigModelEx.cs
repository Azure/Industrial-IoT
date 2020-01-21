// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    /// <summary>
    /// Redundancy model extensions
    /// </summary>
    public static class RedundancyConfigModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static RedundancyConfigModel Clone(this RedundancyConfigModel model) {
            if (model == null) {
                return null;
            }
            return new RedundancyConfigModel {
                DesiredActiveAgents = model.DesiredActiveAgents,
                DesiredPassiveAgents = model.DesiredPassiveAgents
            };
        }
    }
}