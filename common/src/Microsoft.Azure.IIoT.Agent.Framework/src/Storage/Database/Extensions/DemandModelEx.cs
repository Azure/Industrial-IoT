// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Storage.Database {
    using Microsoft.Azure.IIoT.Agent.Framework.Models;

    /// <summary>
    /// Demand model extensions
    /// </summary>
    public static class DemandModelEx {

        /// <summary>
        /// Convert to storage
        /// </summary>
        /// <param name="model"></param>
        /// <param name="jobId"></param>
        /// <returns></returns>
        public static DemandDocument ToDocumentModel(this DemandModel model,
            string jobId) {
            if (model == null) {
                return null;
            }
            return new DemandDocument {
                Key = model.Key,
                Value = model.Value,
                Operator = model.Operator,
                JobId = jobId
            };
        }

        /// <summary>
        /// Convert to Service model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DemandModel ToServiceModel(this DemandDocument model) {
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