// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------


namespace Microsoft.Azure.IIoT.Hub.Mock {
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Shared;

    /// <summary>
    /// Callback
    /// </summary>
    public interface IIoTClientCallback {

        /// <summary>
        /// Call method on client
        /// </summary>
        /// <param name="methodRequest"></param>
        /// <returns></returns>
        MethodResponse Call(MethodRequest methodRequest);

        /// <summary>
        /// Set properties on client
        /// </summary>
        /// <param name="desiredProperties"></param>
        void SetDesiredProperties(TwinCollection desiredProperties);
    }
}
