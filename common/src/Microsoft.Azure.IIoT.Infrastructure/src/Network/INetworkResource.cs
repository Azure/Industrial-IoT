// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Infrastructure.Network {
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// A managed network resource
    /// </summary>
    public interface INetworkResource : IResource {

        /// <summary>
        /// The network address range
        /// </summary>
        IEnumerable<string> AddressSpaces { get; }

        /// <summary>
        /// Network subnet name
        /// </summary>
        string Subnet { get; }

        /// <summary>
        /// Enable inbound port.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        Task TryEnableInboundPortAsync(int port);

        /// <summary>
        /// Disable inbound port.
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        Task TryDisableInboundPortAsync(int port);
    }
}
