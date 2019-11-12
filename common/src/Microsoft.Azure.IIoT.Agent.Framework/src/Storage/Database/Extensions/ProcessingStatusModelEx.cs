// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;

    /// <summary>
    /// Processing status model extensions
    /// </summary>
    public static class ProcessingStatusModelEx {

        /// <summary>
        /// Create document
        /// </summary>
        /// <param name="model"></param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public static ProcessingStatusDocument ToDocumentModel(
            this ProcessingStatusModel model, string jobId) {
            return new ProcessingStatusDocument {
                LastKnownHeartbeat = model.LastKnownHeartbeat,
                LastKnownState = model.LastKnownState,
                ProcessMode = model.ProcessMode,
                JobId = jobId
            };
        }

        /// <summary>
        /// Create framework model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ProcessingStatusModel ToFrameworkModel(
            this ProcessingStatusDocument model) {
            return new ProcessingStatusModel {
                LastKnownHeartbeat = model.LastKnownHeartbeat,
                LastKnownState = model.LastKnownState,
                ProcessMode = model.ProcessMode
            };
        }
    }
}