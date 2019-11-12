// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {

    /// <summary>
    /// Redundancy configuration
    /// </summary>
    public class RedundancyConfigModel {

        /// <summary>
        /// Number of desired active agents
        /// </summary>
        public int DesiredActiveAgents { get; set; }

        /// <summary>
        /// Number of passive agents
        /// </summary>
        public int DesiredPassiveAgents { get; set; }


        /// <inheritdoc/>
        public override bool Equals(object obj) {
            if (!(obj is RedundancyConfigModel objT)) {
                return false;
            }

            return DesiredActiveAgents == objT.DesiredActiveAgents && DesiredPassiveAgents == objT.DesiredPassiveAgents;
        }

        /// <inheritdoc/>
        public override int GetHashCode() {
            return (DesiredActiveAgents + "-" + DesiredPassiveAgents).GetHashCode();
        }
    }
}