// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Microsoft.Azure.IIoT.Net.Dhcp {
    using System.Threading.Tasks;

    /// <summary>
    /// Processes incoming message packets and respond to them. The host
    /// is responsiblé to provide subnet scope, message and responder
    /// context which allows for effective testing and composability.
    /// </summary>
    public interface IDhcpServer {

        /// <summary>
        /// Process packet in context of the specified scope
        /// </summary>
        /// <param name="subnet"></param>
        /// <param name="packet"></param>
        /// <param name="length"></param>
        /// <param name="responder"></param>
        /// <returns></returns>
        Task ProcessMessageAsync(IDhcpScope subnet, byte[] packet,
            int length, IDhcpResponder responder);
    }
}