// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Hub.Models {
    using Microsoft.Azure.Devices;
    using System;

    /// <summary>
    /// Method parameter model extensions
    /// </summary>
    public static class MethodParameterModelEx {

        /// <summary>
        /// Convert model to request
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static CloudToDeviceMethod ToCloudToDeviceMethod(
            this MethodParameterModel model) {
            var methodInfo = new CloudToDeviceMethod(model.Name,
                model.ResponseTimeout ?? TimeSpan.Zero,
                model.ConnectionTimeout ?? TimeSpan.Zero);
            methodInfo.SetPayloadJson(model.JsonPayload);
            return methodInfo;
        }

        /// <summary>
        /// Convert result to model
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        public static MethodParameterModel ToModel(
            this CloudToDeviceMethod method) {
            return new MethodParameterModel {
                JsonPayload = method.GetPayloadAsJson(),
                ConnectionTimeout = method.ConnectionTimeout,
                ResponseTimeout = method.ResponseTimeout,
                Name = method.MethodName
            };
        }

    }
}
