// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Vault {
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Certificate Request event broker
    /// </summary>
    public interface ICertificateRequestEventBroker {

        /// <summary>
        /// Notify all listeners
        /// </summary>
        /// <param name="evt"></param>
        /// <returns></returns>
        Task NotifyAllAsync(Func<ICertificateRequestListener, Task> evt);
    }
}
