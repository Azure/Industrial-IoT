// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace Azure.IIoT.OpcUa.Core.Messaging
{
    using System;

    /// <summary>
    /// Event client factory for creating event clients
    /// </summary>
    public interface IEventClientFactory
    {
        /// <summary>
        /// Name of the technology implementing the event client
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Create a new client using a connection string
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="client"></param>
        /// <returns></returns>
        IDisposable CreateEventClient(string connectionString,
            out IEventClient client);
    }
}
