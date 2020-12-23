// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module.Framework.Services {
    using Microsoft.Azure.Devices.Client;
    using System.Threading.Tasks;

    /// <summary>
    /// Route to the right service
    /// </summary>
    public interface IMethodRouter {

        /// <summary>
        /// Invoke method
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        Task<MethodResponse> InvokeMethodAsync(MethodRequest request);
    }
}
