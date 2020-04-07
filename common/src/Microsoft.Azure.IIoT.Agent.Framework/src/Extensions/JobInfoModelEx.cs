// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Agent.Framework.Models {
    using Microsoft.Azure.IIoT.Serializers;
    using System;
    using System.Linq;

    /// <summary>
    /// Job extensions
    /// </summary>
    public static class JobInfoModelEx {

        /// <summary>
        /// Create hash
        /// </summary>
        /// <returns></returns>
        public static string GetHashSafe(this JobInfoModel model) {
            if (model == null) {
                return "null";
            }
            return model.JobConfiguration.ToString().ToSha1Hash();
        }

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static JobInfoModel Clone(this JobInfoModel model) {
            if (model == null) {
                return null;
            }
            return new JobInfoModel {
                Id = model.Id,
                Name = model.Name,
                Demands = model.Demands?.Select(d => d.Clone()).ToList(),
                JobConfiguration = model.JobConfiguration?.Copy(),
                JobConfigurationType = model.JobConfigurationType,
                LifetimeData = model.LifetimeData?.Clone(),
                RedundancyConfig = model.RedundancyConfig?.Clone(),
            };
        }
    }
}