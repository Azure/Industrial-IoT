// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    /// <summary>
    /// Processing status model extensions
    /// </summary>
    public static class ProcessingStatusModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ProcessingStatusModel Clone(this ProcessingStatusModel model) {
            if (model == null) {
                return null;
            }
            return new ProcessingStatusModel {
                LastKnownHeartbeat = model.LastKnownHeartbeat,
                LastKnownState = model.LastKnownState,
                ProcessMode = model.ProcessMode
            };
        }
    }
}