// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.Devices;

    /// <summary>
    /// Configuration metrics model extensions
    /// </summary>
    public static class ConfigurationMetricsModelEx {

        /// <summary>
        /// Convert configuration metrics model to
        /// configuration metrics
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ConfigurationMetrics ToContent(this ConfigurationMetricsModel model) {
            return new ConfigurationMetrics {
                Queries = model.Queries,
                Results = model.Results
            };
        }

        /// <summary>
        /// Convert configuration metrics to model
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static ConfigurationMetricsModel ToModel(this ConfigurationMetrics content) {
            return new ConfigurationMetricsModel {
                Queries = content.Queries,
                Results = content.Results
            };
        }
    }
}
