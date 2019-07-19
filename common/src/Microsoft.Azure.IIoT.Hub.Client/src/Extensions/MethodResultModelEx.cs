// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.Devices;

    /// <summary>
    /// Method result model extensions
    /// </summary>
    public static class MethodResultModelEx {

        /// <summary>
        /// Convert result to model
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static MethodResultModel ToModel(this CloudToDeviceMethodResult result) {
            return new MethodResultModel {
                JsonPayload = result.GetPayloadAsJson(),
                Status = result.Status
            };
        }

        /// <summary>
        /// Convert result to model
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public static MethodResultModel ToModel(this DeviceJobOutcome result) {
            return new MethodResultModel {
                JsonPayload = result.DeviceMethodResponse.GetPayloadAsJson(),
                Status = result.DeviceMethodResponse.Status
            };
        }
    }
}
