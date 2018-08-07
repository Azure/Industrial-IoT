// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry.Services {
    using Microsoft.Azure.IIoT.Exceptions;
    using Microsoft.Azure.IIoT.Hub;
    using Microsoft.Azure.IIoT.Hub.Models;
    using System;
    using System.Threading.Tasks;

    public static class OpcUaOnboardingHelper {

        public const string kId = "215DD51095C847C7B4311D683F782069";

        /// <summary>
        /// Ensure the onboarder identity exists so that we can
        /// send messages to it.
        /// </summary>
        /// <returns></returns>
        public static async Task EnsureOnboarderIdExists(IIoTHubTwinServices iothub) {
            if (iothub == null) {
                throw new ArgumentNullException(nameof(iothub));
            }
            try {
                await iothub.GetAsync(kId);
            }
            catch (ResourceNotFoundException) {
                await iothub.CreateOrUpdateAsync(new DeviceTwinModel {
                    Id = kId
                });
            }
        }
    }
}
