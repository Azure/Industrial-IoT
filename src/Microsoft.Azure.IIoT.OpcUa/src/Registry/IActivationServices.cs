// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using System.Threading.Tasks;

    /// <summary>
    /// Twin activation services
    /// </summary>
    public interface IActivationServices<T> {

        /// <summary>
        /// Activate twin
        /// </summary>
        /// <param name="id"></param>
        /// <param name="secret"></param>
        /// <returns></returns>
        Task ActivateTwinAsync(T id, string secret);

        /// <summary>
        /// Deactivate twin
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task DeactivateTwinAsync(T id);
    }
}
