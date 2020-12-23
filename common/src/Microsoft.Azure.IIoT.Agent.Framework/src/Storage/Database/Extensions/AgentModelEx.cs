// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;

    /// <summary>
    /// Agent model extensions
    /// </summary>
    public static class AgentModelEx {

        /// <summary>
        /// Convert to storage
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WorkerDocument ToDocumentModel(this WorkerInfoModel model) {
            if (model == null) {
                return null;
            }
            return new WorkerDocument {
                Id = model.AgentId,
                WorkerStatus = model.Status,
                LastSeen = model.LastSeen
            };
        }

        /// <summary>
        /// Convert to Service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WorkerInfoModel ToFrameworkModel(this WorkerDocument model) {
            if (model == null) {
                return null;
            }
            return new WorkerInfoModel {
                AgentId = model.Id,
                Status = model.WorkerStatus,
                LastSeen = model.LastSeen
            };
        }
    }
}