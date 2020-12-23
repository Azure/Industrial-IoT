// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.OpcUa.Registry {
    using Microsoft.Azure.IIoT.OpcUa.Registry.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery process processing
    /// </summary>
    public interface IDiscoveryProgressProcessor {

        /// <summary>
        /// Handle discovery progress messages
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task OnDiscoveryProgressAsync(DiscoveryProgressModel message);
    }
}
