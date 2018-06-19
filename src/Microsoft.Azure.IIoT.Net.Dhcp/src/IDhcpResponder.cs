// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp {
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Responder allowing protocol to respond to message
    /// </summary>
    public interface IDhcpResponder {

        /// <summary>
        /// Check whether address is assigned
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        Task<bool> IsAddressAssignedAsync(IPAddress address);

        /// <summary>
        /// Send response
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="length"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        Task SendResponseAsync(byte[] packet, int length,
            IPAddress address);
    }
}