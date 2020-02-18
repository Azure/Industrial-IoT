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
        public static WorkerSupervisorDocument ToDocumentModel(this WorkerSupervisorInfoModel model) {
            if (model == null) {
                return null;
            }
            return new WorkerSupervisorDocument {
                Id = model.WorkerSupervisorId,
                WorkerStatus = model.Status,
                LastSeen = model.LastSeen
            };
        }

        /// <summary>
        /// Convert to Service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static WorkerSupervisorInfoModel ToFrameworkModel(this WorkerSupervisorDocument model) {
            if (model == null) {
                return null;
            }
            return new WorkerSupervisorInfoModel {
                WorkerSupervisorId = model.Id,
                Status = model.WorkerStatus,
                LastSeen = model.LastSeen
            };
        }
    }
}