// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using System;

    /// <summary>
    /// Device model extensions
    /// </summary>
    public static class DeviceModelEx {

        /// <summary>
        /// Check whether device is connected
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static bool? IsConnected(this DeviceModel model) {
            return model.ConnectionState?.EqualsIgnoreCase("Connected");
        }

        /// <summary>
        /// Clone twin
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DeviceModel Clone(this DeviceModel model) {
            if (model == null) {
                return null;
            }
            return new DeviceModel {
                Etag = model.Etag,
                Id = model.Id,
                ModuleId = model.ModuleId,
                ConnectionState = model.ConnectionState,
                Authentication = model.Authentication.Clone()
            };
        }

        /// <summary>
        /// Clone authentication
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static DeviceAuthenticationModel Clone(this DeviceAuthenticationModel model) {
            if (model == null) {
                return null;
            }
            return new DeviceAuthenticationModel {
                PrimaryKey = model.PrimaryKey,
                SecondaryKey = model.SecondaryKey
            };
        }
    }
}
