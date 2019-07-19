// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.Devices;

    /// <summary>
    /// Configuration content model extensions
    /// </summary>
    public static class ConfigurationContentModelEx {

        /// <summary>
        /// Convert configuration content model to
        /// configuration content
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static ConfigurationContent ToContent(this ConfigurationContentModel model) {
            return new ConfigurationContent {
                ModulesContent = model.ModulesContent,
                DeviceContent = model.DeviceContent
            };
        }

        /// <summary>
        /// Convert configuration content to model
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static ConfigurationContentModel ToModel(this ConfigurationContent content) {
            return new ConfigurationContentModel {
                ModulesContent = content.ModulesContent,
                DeviceContent = content.DeviceContent
            };
        }
    }
}
