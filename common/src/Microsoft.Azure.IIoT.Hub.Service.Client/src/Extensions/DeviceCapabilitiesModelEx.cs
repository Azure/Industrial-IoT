// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.Devices.Shared;

    /// <summary>
    /// Device capabilities model extensions
    /// </summary>
    public static class DeviceCapabilitiesModelEx {

        /// <summary>
        /// Convert capabilities model to capabilities
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DeviceCapabilities ToCapabilities(this DeviceCapabilitiesModel model) {
            return new DeviceCapabilities {
                IotEdge = model.IotEdge ?? false
            };
        }

        /// <summary>
        /// Convert capabilities to model
        /// </summary>
        /// <param name="capabilities"></param>
        /// <returns></returns>
        public static DeviceCapabilitiesModel ToModel(this DeviceCapabilities capabilities) {
            return new DeviceCapabilitiesModel {
                IotEdge = capabilities.IotEdge
            };
        }
    }
}
