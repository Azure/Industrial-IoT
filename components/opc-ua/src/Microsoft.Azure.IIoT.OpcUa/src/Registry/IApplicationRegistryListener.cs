// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Application registry change listener
    /// </summary>
    public interface IApplicationRegistryListener {

        /// <summary>
        /// Called when application is added
        /// </summary>
        /// <param name="context"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        Task OnApplicationNewAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application);

        /// <summary>
        /// Called when application is updated
        /// </summary>
        /// <param name="context"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        Task OnApplicationUpdatedAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application);

        /// <summary>
        /// Called when application is enabled
        /// </summary>
        /// <param name="context"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        Task OnApplicationEnabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application);

        /// <summary>
        /// Called when application is disabled
        /// </summary>
        /// <param name="context"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        Task OnApplicationDisabledAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application);

        /// <summary>
        /// Called when application is unregistered
        /// </summary>
        /// <param name="context"></param>
        /// <param name="application"></param>
        /// <returns></returns>
        Task OnApplicationDeletedAsync(RegistryOperationContextModel context,
            ApplicationInfoModel application);
    }
}
