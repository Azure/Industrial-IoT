// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using System.Linq;

    /// <summary>
    /// Lifetime data model extensions
    /// </summary>
    public static class JobLifetimeDataModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static JobLifetimeDataModel Clone(this JobLifetimeDataModel model) {
            if (model == null) {
                return null;
            }
            return new JobLifetimeDataModel {
                Created = model.Created,
                ProcessingStatus = model.ProcessingStatus?.ToDictionary(k => k.Key, v => v.Value.Clone()),
                Status = model.Status,
                Updated = model.Updated
            };
        }
    }
}