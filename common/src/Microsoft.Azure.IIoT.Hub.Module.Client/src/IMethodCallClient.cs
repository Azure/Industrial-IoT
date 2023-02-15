// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client {
    using Microsoft.Azure.Devices.Client;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Method call client
    /// </summary>
    public interface IMethodCallClient {
        /// <summary>
        /// Invoke a method on device or module
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="moduleId"></param>
        /// <param name="methodRequest"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<MethodResponse> InvokeMethodAsync(string deviceId, string moduleId,
            MethodRequest methodRequest,
            CancellationToken cancellationToken = default);
    }
}
