// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Module {
    using Microsoft.Azure.IIoT.Exceptions;
    using System.Threading.Tasks;

    /// <summary>
    /// Handles method call invocation
    /// </summary>
    public interface IMethodHandler {

        /// <summary>
        /// Method handler
        /// </summary>
        /// <param name="method"></param>
        /// <param name="payload"></param>
        /// <param name="contentType"></param>
        /// <exception cref="MethodCallStatusException"/>
        /// <returns></returns>
        Task<byte[]> InvokeAsync(string method, byte[] payload,
            string contentType);
    }
}
