// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Publisher.Models {
    /// <summary>
    /// Settings extensions
    /// </summary>
    public static class PublishedDataSetSettingsModelEx {

        /// <summary>
        /// Clone
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static PublishedDataSetSettingsModel Clone(this PublishedDataSetSettingsModel model) {
            if (model == null) {
                return null;
            }
            return new PublishedDataSetSettingsModel {
                LifeTimeCount = model.LifeTimeCount,
                MaxKeepAliveCount = model.MaxKeepAliveCount,
                MaxNotificationsPerPublish = model.MaxNotificationsPerPublish,
                Priority = model.Priority,
                PublishingInterval = model.PublishingInterval,
                ResolveDisplayName = model.ResolveDisplayName
            };
        }
    }
}