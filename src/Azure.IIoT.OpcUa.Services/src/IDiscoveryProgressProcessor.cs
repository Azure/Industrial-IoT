// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Services
{
    using Azure.IIoT.OpcUa.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Discovery process processing
    /// </summary>
    public interface IDiscoveryProgressProcessor
    {
        /// <summary>
        /// Handle discovery progress messages
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task OnDiscoveryProgressAsync(DiscoveryProgressModel message);
    }
}
