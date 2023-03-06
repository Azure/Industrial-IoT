// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Client
{
    using Microsoft.Azure.Devices.Client;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Method call client
    /// </summary>
    public interface IMethodCallClient
    {
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

        /// <summary>
        /// Registers a new delegate that is called for a method that
        /// doesn't have a delegate registered for its name.
        /// If a default delegate is already registered it will replace
        /// with the new delegate.
        /// </summary>
        /// <param name="methodHandler">The delegate to be used when
        /// a method is called by the cloud service and there is no
        /// delegate registered for that method name.</param>
        Task SetMethodHandlerAsync(MethodCallback methodHandler);
    }
}
