// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.Devices;

    /// <summary>
    /// Configuration model extensions
    /// </summary>
    public static class ConfigurationModelEx {

        /// <summary>
        /// Convert configuration model to configuration
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static Configuration ToConfiguration(this ConfigurationModel config) {
            return new Configuration(config.Id) {
                Content = config.Content.ToContent(),
                ETag = config.Etag,
                Labels = config.Labels,
                Priority = config.Priority,
                TargetCondition = config.TargetCondition
            };
        }

        /// <summary>
        /// Convert configuration to model
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static ConfigurationModel ToModel(this Configuration config) {
            return new ConfigurationModel {
                Id = config.Id,
                Etag = config.ETag,
                ContentType = config.ContentType,
                TargetCondition = config.TargetCondition,
                Priority = config.Priority,
                Labels = config.Labels,
                Content = config.Content.ToModel(),
                CreatedTimeUtc = config.CreatedTimeUtc,
                LastUpdatedTimeUtc = config.LastUpdatedTimeUtc,
                Metrics = config.Metrics.ToModel(),
                SchemaVersion = config.SchemaVersion,
                SystemMetrics = config.SystemMetrics.ToModel()
            };
        }
    }
}
